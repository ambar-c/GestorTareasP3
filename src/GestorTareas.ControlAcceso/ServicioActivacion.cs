using GestorTareas.Notificaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorTareas.ControlAcceso;

public sealed class ServicioActivacion(
    ContextoControlAcceso contexto,
    IColaCorreos colaCorreos,
    OpcionesActivacion opcionesActivacion,
    ILogger<ServicioActivacion> logger)
{
    private const string MensajeEnlaceInvalido = "El enlace de activación no es válido o ya venció.";
    private const string MensajeErrorInterno = "No se pudo completar la operación. Inténtalo de nuevo.";
    private const string MensajeReenvioAceptado = "Si el correo corresponde a una cuenta pendiente de activación, recibirás un nuevo enlace.";

    public async Task<ResultadoActivacion> ActivarAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return new ResultadoActivacion(EstadoActivacion.EnlaceInvalido, MensajeEnlaceInvalido);
        }

        try
        {
            byte[] hash = GeneradorTokenActivacion.Hashear(token);
            DateTime ahora = DateTime.UtcNow;

            int filasAfectadas = await contexto.Usuarios
                .Where(usuario => usuario.HashTokenActivacion == hash
                    && usuario.VencimientoActivacion >= ahora
                    && !usuario.Activo
                    && !usuario.Desactivado)
                .ExecuteUpdateAsync(actualizacion => actualizacion
                    .SetProperty(usuario => usuario.Activo, true)
                    .SetProperty(usuario => usuario.HashTokenActivacion, (byte[]?)null)
                    .SetProperty(usuario => usuario.VencimientoActivacion, (DateTime?)null));

            return filasAfectadas == 1
                ? new ResultadoActivacion(EstadoActivacion.Activada, "Cuenta activada correctamente.")
                : new ResultadoActivacion(EstadoActivacion.EnlaceInvalido, MensajeEnlaceInvalido);
        }
        catch (Exception excepcion)
        {
            logger.LogError(excepcion, MensajeErrorInterno);
            return new ResultadoActivacion(EstadoActivacion.ErrorInterno, MensajeErrorInterno);
        }
    }

    public async Task<ResultadoReenvio> ReenviarAsync(string? correo)
    {
        try
        {
            ResultadoValidacionRegistro validacion = ValidadorRegistro.ValidarCorreo(correo);
            if (!validacion.EsValido)
            {
                return new ResultadoReenvio(
                    EstadoReenvio.DatosInvalidos,
                    string.Empty,
                    validacion.Errores);
            }

            Usuario? usuario = await contexto.Usuarios
                .SingleOrDefaultAsync(usuario => usuario.Correo == validacion.CorreoNormalizado);

            if (usuario is not null && !usuario.Activo && !usuario.Desactivado)
            {
                (string token, byte[] hash) = GeneradorTokenActivacion.Generar();
                usuario.HashTokenActivacion = hash;
                usuario.VencimientoActivacion = DateTime.UtcNow.AddHours(opcionesActivacion.HorasVigencia);
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
            }

            return new ResultadoReenvio(
                EstadoReenvio.Aceptado,
                MensajeReenvioAceptado,
                Array.Empty<string>());
        }
        catch (Exception excepcion)
        {
            logger.LogError(excepcion, MensajeErrorInterno);
            return new ResultadoReenvio(
                EstadoReenvio.ErrorInterno,
                MensajeErrorInterno,
                Array.Empty<string>());
        }
    }
}
