using GestorTareas.GestionPermisos.Entidades;
using GestorTareas.GestionPermisos.Estados;
using GestorTareas.GestionPermisos.Permisos;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.GestionPermisos;

public sealed record ResultadoCrearSolicitudPermiso(
    bool Exito,
    string? Error,
    SolicitudPermiso? Solicitud);

public enum EstadoResolucionSolicitudPermiso
{
    Exitoso,
    SolicitudNoEncontrada,
    MismoSolicitante,
    TransicionNoValida,
    MotivoRechazoInvalido
}

public sealed record ResultadoResolucionSolicitudPermiso(
    EstadoResolucionSolicitudPermiso Estado,
    string? Motivo,
    SolicitudPermiso? Solicitud);

public sealed class ServicioSolicitudesPermiso(ContextoGestionPermisos contexto)
{
    public async Task<ResultadoCrearSolicitudPermiso> CrearAsync(
        Guid idSolicitante,
        string? permisoSolicitado)
    {
        string? nombrePermiso = permisoSolicitado?.Trim();
        if (string.IsNullOrWhiteSpace(nombrePermiso)
            || !Enum.GetNames<Permiso>().Any(nombre =>
                string.Equals(nombre, nombrePermiso, StringComparison.OrdinalIgnoreCase)))
        {
            return new ResultadoCrearSolicitudPermiso(
                false,
                "El permiso solicitado no es válido.",
                null);
        }

        Enum.TryParse(nombrePermiso, true, out Permiso permiso);

        var solicitud = new SolicitudPermiso
        {
            Id = Guid.NewGuid(),
            IdSolicitante = idSolicitante,
            PermisoSolicitado = permiso,
            Estado = EstadoSolicitudPermiso.Pendiente,
            FechaCreacion = DateTime.UtcNow
        };

        contexto.SolicitudesPermiso.Add(solicitud);
        await contexto.SaveChangesAsync();

        return new ResultadoCrearSolicitudPermiso(true, null, solicitud);
    }

    public async Task<ResultadoResolucionSolicitudPermiso> AprobarAsync(
        Guid idSolicitud,
        Guid idAdministrador)
    {
        SolicitudPermiso? solicitud = await contexto.SolicitudesPermiso
            .SingleOrDefaultAsync(solicitud => solicitud.Id == idSolicitud);

        if (solicitud is null)
        {
            return new ResultadoResolucionSolicitudPermiso(
                EstadoResolucionSolicitudPermiso.SolicitudNoEncontrada,
                "Solicitud de permiso no encontrada.",
                null);
        }

        if (solicitud.IdSolicitante == idAdministrador)
        {
            return new ResultadoResolucionSolicitudPermiso(
                EstadoResolucionSolicitudPermiso.MismoSolicitante,
                "El Administrador no puede resolver su propia solicitud.",
                null);
        }

        if (!MaquinaEstadosSolicitud.EsTransicionValida(
                solicitud.Estado,
                EstadoSolicitudPermiso.Aprobada))
        {
            return new ResultadoResolucionSolicitudPermiso(
                EstadoResolucionSolicitudPermiso.TransicionNoValida,
                "La transición de estado no es válida.",
                null);
        }

        if (!MaquinaEstadosSolicitud.EsTransicionValida(
                EstadoSolicitudPermiso.Aprobada,
                EstadoSolicitudPermiso.Aplicada))
        {
            return new ResultadoResolucionSolicitudPermiso(
                EstadoResolucionSolicitudPermiso.TransicionNoValida,
                "La transición de estado no es válida.",
                null);
        }

        bool yaTienePermiso = await TienePermisoAsync(
            solicitud.IdSolicitante,
            solicitud.PermisoSolicitado);

        solicitud.Estado = EstadoSolicitudPermiso.Aplicada;
        solicitud.IdAprobador = idAdministrador;
        solicitud.FechaResolucion = DateTime.UtcNow;

        if (!yaTienePermiso)
        {
            contexto.PermisosOtorgados.Add(new PermisoOtorgado
            {
                Id = Guid.NewGuid(),
                IdUsuario = solicitud.IdSolicitante,
                Permiso = solicitud.PermisoSolicitado,
                FechaOtorgamiento = DateTime.UtcNow
            });
        }

        await contexto.SaveChangesAsync();

        return new ResultadoResolucionSolicitudPermiso(
            EstadoResolucionSolicitudPermiso.Exitoso,
            null,
            solicitud);
    }

    public Task<bool> TienePermisoAsync(
        Guid idUsuario,
        Permiso permiso)
    {
        return contexto.PermisosOtorgados
            .AnyAsync(permisoOtorgado =>
                permisoOtorgado.IdUsuario == idUsuario
                && permisoOtorgado.Permiso == permiso);
    }

    public async Task<ResultadoResolucionSolicitudPermiso> RechazarAsync(
        Guid idSolicitud,
        Guid idAdministrador,
        string? motivo)
    {
        SolicitudPermiso? solicitud = await contexto.SolicitudesPermiso
            .SingleOrDefaultAsync(solicitud => solicitud.Id == idSolicitud);

        if (solicitud is null)
        {
            return new ResultadoResolucionSolicitudPermiso(
                EstadoResolucionSolicitudPermiso.SolicitudNoEncontrada,
                "Solicitud de permiso no encontrada.",
                null);
        }

        if (solicitud.IdSolicitante == idAdministrador)
        {
            return new ResultadoResolucionSolicitudPermiso(
                EstadoResolucionSolicitudPermiso.MismoSolicitante,
                "El Administrador no puede resolver su propia solicitud.",
                null);
        }

        if (!MaquinaEstadosSolicitud.EsTransicionValida(
                solicitud.Estado,
                EstadoSolicitudPermiso.Rechazada))
        {
            return new ResultadoResolucionSolicitudPermiso(
                EstadoResolucionSolicitudPermiso.TransicionNoValida,
                "La transición de estado no es válida.",
                null);
        }

        string? motivoNormalizado = motivo?.Trim();
        if (string.IsNullOrWhiteSpace(motivoNormalizado))
        {
            return new ResultadoResolucionSolicitudPermiso(
                EstadoResolucionSolicitudPermiso.MotivoRechazoInvalido,
                "Debes indicar el motivo del rechazo.",
                null);
        }

        solicitud.Estado = EstadoSolicitudPermiso.Rechazada;
        solicitud.IdAprobador = idAdministrador;
        solicitud.FechaResolucion = DateTime.UtcNow;
        solicitud.MotivoRechazo = motivoNormalizado;

        await contexto.SaveChangesAsync();

        return new ResultadoResolucionSolicitudPermiso(
            EstadoResolucionSolicitudPermiso.Exitoso,
            null,
            solicitud);
    }

    public Task<List<SolicitudPermiso>> ListarPendientesAsync()
    {
        return contexto.SolicitudesPermiso
            .AsNoTracking()
            .Where(solicitud => solicitud.Estado == EstadoSolicitudPermiso.Pendiente)
            .OrderBy(solicitud => solicitud.FechaCreacion)
            .ToListAsync();
    }

    public Task<List<SolicitudPermiso>> ListarPropiasAsync(Guid idSolicitante)
    {
        return contexto.SolicitudesPermiso
            .AsNoTracking()
            .Where(solicitud => solicitud.IdSolicitante == idSolicitante)
            .OrderByDescending(solicitud => solicitud.FechaCreacion)
            .ToListAsync();
    }
}
