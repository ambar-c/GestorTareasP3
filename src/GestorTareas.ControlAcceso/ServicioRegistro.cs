using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GestorTareas.Notificaciones;

namespace GestorTareas.ControlAcceso;

public sealed class ServicioRegistro(
    ContextoControlAcceso contexto,
    ILogger<ServicioRegistro> logger,
    IColaCorreos colaCorreos,
    OpcionesActivacion opcionesActivacion)
{
    private const string MensajeCorreoDuplicado = "Ya existe una cuenta con ese correo.";
    private const string MensajeErrorInterno = "No se pudo completar el registro. Inténtalo de nuevo.";

    public async Task<ResultadoRegistro> RegistrarAsync(
        string? nombre,
        string? correo,
        string? contrasena)
    {
        try
        {
            ResultadoValidacionRegistro validacion = ValidadorRegistro.Validar(
                nombre,
                correo,
                contrasena);

            if (!validacion.EsValido)
            {
                return new ResultadoRegistro(
                    EstadoRegistro.DatosInvalidos,
                    validacion.Errores);
            }

            bool correoYaRegistrado = await contexto.Usuarios.AnyAsync(
                usuario => usuario.Correo == validacion.CorreoNormalizado);

            if (correoYaRegistrado)
            {
                return CorreoDuplicado();
            }

            (byte[] sal, byte[] hash) = HasheadorContrasena.Hashear(contrasena!);
            (string token, byte[] hashToken) = GeneradorTokenActivacion.Generar();
            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nombre = nombre!.Trim(),
                Correo = validacion.CorreoNormalizado,
                HashContrasena = hash,
                Sal = sal,
                Rol = Rol.Estandar,
                Activo = false,
                FechaCreacion = DateTime.UtcNow,
                HashTokenActivacion = hashToken,
                VencimientoActivacion = DateTime.UtcNow.AddHours(opcionesActivacion.HorasVigencia)
            };

            contexto.Usuarios.Add(usuario);
            await contexto.SaveChangesAsync();

            try
            {
                (string asunto, string cuerpo) = PlantillaCorreoActivacion.Construir(
                    usuario.Nombre,
                    opcionesActivacion.UrlBase,
                    token);
                await colaCorreos.EncolarAsync(usuario.Correo, asunto, cuerpo);
            }
            catch (Exception excepcion)
            {
                logger.LogError(excepcion, "No se pudo encolar el correo de activación.");
            }

            return new ResultadoRegistro(
                EstadoRegistro.Creado,
                Array.Empty<string>(),
                usuario.Id);
        }
        catch (DbUpdateException excepcion) when (EsViolacionIndiceUnico(excepcion))
        {
            return CorreoDuplicado();
        }
        catch (Exception excepcion)
        {
            logger.LogError(excepcion, "Error inesperado al registrar un usuario.");

            return new ResultadoRegistro(
                EstadoRegistro.ErrorInterno,
                [MensajeErrorInterno]);
        }
    }

    private static ResultadoRegistro CorreoDuplicado()
    {
        return new ResultadoRegistro(
            EstadoRegistro.CorreoDuplicado,
            [MensajeCorreoDuplicado]);
    }

    private static bool EsViolacionIndiceUnico(DbUpdateException excepcion)
    {
        return excepcion.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
