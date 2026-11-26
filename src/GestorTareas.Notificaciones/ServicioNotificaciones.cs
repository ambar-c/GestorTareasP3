using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Notificaciones;

public enum EstadoMarcarNotificacion
{
    Exitoso,
    NotificacionNoEncontrada,
    NoPerteneceAlUsuario
}

public sealed record ResultadoMarcarNotificacion(
    EstadoMarcarNotificacion Estado,
    Notificacion? Notificacion);

public sealed class ServicioNotificaciones(
    ContextoNotificaciones contexto,
    IColaCorreos colaCorreos)
{
    public async Task<Notificacion> NotificarAsync(
        Guid usuarioId,
        string correo,
        string titulo,
        string mensaje)
    {
        var notificacion = new Notificacion
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Titulo = titulo,
            Mensaje = mensaje,
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        };

        contexto.Notificaciones.Add(notificacion);
        await contexto.SaveChangesAsync();

        bool correoActivo = await contexto.PreferenciasNotificacion
            .Where(preferencia => preferencia.UsuarioId == usuarioId)
            .Select(preferencia => preferencia.CorreoActivo)
            .SingleOrDefaultAsync();

        bool existePreferencia = await contexto.PreferenciasNotificacion
            .AnyAsync(preferencia => preferencia.UsuarioId == usuarioId);

        if (ValidadorCorreo.EsValido(correo)
            && (!existePreferencia || correoActivo))
        {
            await colaCorreos.EncolarAsync(correo, titulo, mensaje);
        }

        return notificacion;
    }

    public async Task<List<Notificacion>> ListarAsync(
        Guid usuarioId,
        bool? leida = null)
    {
        IQueryable<Notificacion> consulta = contexto.Notificaciones
            .AsNoTracking()
            .Where(notificacion => notificacion.UsuarioId == usuarioId);

        if (leida.HasValue)
        {
            consulta = consulta.Where(notificacion => notificacion.Leida == leida.Value);
        }

        return await consulta
            .OrderByDescending(notificacion => notificacion.FechaCreacion)
            .ToListAsync();
    }

    public async Task<ResultadoMarcarNotificacion> MarcarComoLeidaAsync(
        Guid idNotificacion,
        Guid usuarioId)
    {
        Notificacion? notificacion = await contexto.Notificaciones
            .SingleOrDefaultAsync(notificacion => notificacion.Id == idNotificacion);

        if (notificacion is null)
        {
            return new ResultadoMarcarNotificacion(
                EstadoMarcarNotificacion.NotificacionNoEncontrada,
                null);
        }

        if (notificacion.UsuarioId != usuarioId)
        {
            return new ResultadoMarcarNotificacion(
                EstadoMarcarNotificacion.NoPerteneceAlUsuario,
                null);
        }

        notificacion.Leida = true;
        await contexto.SaveChangesAsync();

        return new ResultadoMarcarNotificacion(
            EstadoMarcarNotificacion.Exitoso,
            notificacion);
    }

    public async Task<PreferenciaNotificacion> GuardarPreferenciaAsync(
        Guid usuarioId,
        bool correoActivo)
    {
        PreferenciaNotificacion? preferencia = await contexto.PreferenciasNotificacion
            .SingleOrDefaultAsync(preferencia => preferencia.UsuarioId == usuarioId);

        if (preferencia is null)
        {
            preferencia = new PreferenciaNotificacion
            {
                UsuarioId = usuarioId,
                CorreoActivo = correoActivo
            };
            contexto.PreferenciasNotificacion.Add(preferencia);
        }
        else
        {
            preferencia.CorreoActivo = correoActivo;
        }

        await contexto.SaveChangesAsync();
        return preferencia;
    }
}
