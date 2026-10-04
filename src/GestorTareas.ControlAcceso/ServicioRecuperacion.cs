using GestorTareas.Notificaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorTareas.ControlAcceso;

public sealed class ServicioRecuperacion(
    ContextoControlAcceso contexto,
    IColaCorreos colaCorreos,
    ILogger<ServicioRecuperacion> logger,
    ServicioContrasenas servicioContrasenas)
{
    public async Task EmitirCodigoAsync(Usuario usuario)
    {
        await contexto.CodigosRecuperacion
            .Where(codigo => codigo.UsuarioId == usuario.Id && !codigo.Usado)
            .ExecuteUpdateAsync(actualizacion => actualizacion
                .SetProperty(codigo => codigo.Usado, true));

        (string codigo, byte[] hash) = GeneradorTokenActivacion.Generar();
        DateTime ahora = DateTime.UtcNow;

        contexto.CodigosRecuperacion.Add(new CodigoRecuperacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            HashCodigo = hash,
            FechaEmision = ahora,
            FechaVencimiento = ahora.AddMinutes(30),
            Usado = false
        });
        await contexto.SaveChangesAsync();

        try
        {
            (string asunto, string cuerpo) = PlantillaCorreoRecuperacion.Construir(
                usuario.Nombre,
                codigo);
            await colaCorreos.EncolarAsync(usuario.Correo, asunto, cuerpo);
        }
        catch (Exception excepcion)
        {
            logger.LogError(excepcion, "No se pudo encolar el correo de recuperación.");
        }
    }

    public async Task SolicitarAsync(string? correo)
    {
        ResultadoValidacionRegistro validacion = ValidadorRegistro.ValidarCorreo(correo);
        if (!validacion.EsValido)
        {
            return;
        }

        Usuario? usuario = await contexto.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Correo == validacion.CorreoNormalizado);

        if (usuario is not null && usuario.Activo && !usuario.Desactivado)
        {
            await EmitirCodigoAsync(usuario);
        }
    }

    public async Task<ResultadoRestablecimiento> RestablecerAsync(
        string? codigo,
        string? contrasenaNueva)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return ResultadoRestablecimiento.CodigoInvalido();
        }

        byte[] hashCodigo = GeneradorTokenActivacion.Hashear(codigo);
        DateTime ahora = DateTime.UtcNow;
        CodigoRecuperacion? codigoRecuperacion = await contexto.CodigosRecuperacion
            .Include(codigoAlmacenado => codigoAlmacenado.Usuario)
            .SingleOrDefaultAsync(codigoAlmacenado =>
                codigoAlmacenado.HashCodigo == hashCodigo
                && !codigoAlmacenado.Usado
                && codigoAlmacenado.FechaVencimiento > ahora
                && codigoAlmacenado.Usuario.Activo
                && !codigoAlmacenado.Usuario.Desactivado);

        if (codigoRecuperacion is null)
        {
            return ResultadoRestablecimiento.CodigoInvalido();
        }

        IReadOnlyList<string> erroresContrasena =
            ValidadorRegistro.ValidarContrasena(contrasenaNueva);
        if (erroresContrasena.Count > 0)
        {
            return ResultadoRestablecimiento.DatosInvalidos(erroresContrasena);
        }

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaccion =
            await contexto.Database.BeginTransactionAsync();

        codigoRecuperacion.Usado = true;
        ResultadoEstablecerContrasena resultado = await servicioContrasenas
            .EstablecerContrasenaAsync(codigoRecuperacion.Usuario, contrasenaNueva);

        if (!resultado.Exitoso)
        {
            await transaccion.RollbackAsync();
            return ResultadoRestablecimiento.DatosInvalidos(resultado.Errores);
        }

        await transaccion.CommitAsync();
        return ResultadoRestablecimiento.Exitoso();
    }
}

public enum EstadoRestablecimiento
{
    Exitoso,
    DatosInvalidos,
    CodigoInvalido
}

public sealed record ResultadoRestablecimiento(
    EstadoRestablecimiento Estado,
    IReadOnlyList<string> Errores)
{
    private const string MensajeCodigoInvalido = "El código no es válido o ha vencido.";

    public static ResultadoRestablecimiento Exitoso() =>
        new(EstadoRestablecimiento.Exitoso, Array.Empty<string>());

    public static ResultadoRestablecimiento DatosInvalidos(IReadOnlyList<string> errores) =>
        new(EstadoRestablecimiento.DatosInvalidos, errores);

    public static ResultadoRestablecimiento CodigoInvalido() =>
        new(EstadoRestablecimiento.CodigoInvalido, [MensajeCodigoInvalido]);
}
