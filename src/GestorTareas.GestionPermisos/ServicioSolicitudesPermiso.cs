using GestorTareas.GestionPermisos.Entidades;
using GestorTareas.GestionPermisos.Estados;
using GestorTareas.GestionPermisos.Permisos;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.GestionPermisos;

public sealed record ResultadoCrearSolicitudPermiso(
    bool Exito,
    string? Error,
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
