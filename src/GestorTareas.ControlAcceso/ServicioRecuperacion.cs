using GestorTareas.Notificaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorTareas.ControlAcceso;

public sealed class ServicioRecuperacion(
    ContextoControlAcceso contexto,
    IColaCorreos colaCorreos,
    ILogger<ServicioRecuperacion> logger)
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
}
