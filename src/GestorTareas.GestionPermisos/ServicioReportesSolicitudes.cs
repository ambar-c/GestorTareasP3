using GestorTareas.GestionPermisos.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.GestionPermisos;

public sealed record ReporteSolicitudPermiso(
    string Periodo,
    string Estado,
    int Total);

public sealed class ServicioReportesSolicitudes(ContextoGestionPermisos contexto)
{
    public async Task<IReadOnlyList<ReporteSolicitudPermiso>> ObtenerAsync(
        Guid idUsuario,
        bool esAdministrador,
        DateTime? desde,
        DateTime? hasta)
    {
        IQueryable<SolicitudPermiso> solicitudes = contexto.SolicitudesPermiso.AsNoTracking();

        if (!esAdministrador)
        {
            solicitudes = solicitudes.Where(solicitud => solicitud.IdSolicitante == idUsuario);
        }

        if (desde.HasValue)
        {
            solicitudes = solicitudes.Where(solicitud => solicitud.FechaCreacion >= desde.Value);
        }

        if (hasta.HasValue)
        {
            solicitudes = solicitudes.Where(solicitud => solicitud.FechaCreacion < hasta.Value);
        }

        var grupos = await solicitudes
            .GroupBy(solicitud => new
            {
                Anio = solicitud.FechaCreacion.Year,
                Mes = solicitud.FechaCreacion.Month,
                solicitud.Estado
            })
            .Select(grupo => new
            {
                grupo.Key.Anio,
                grupo.Key.Mes,
                grupo.Key.Estado,
                Total = grupo.Count()
            })
            .OrderBy(grupo => grupo.Anio)
            .ThenBy(grupo => grupo.Mes)
            .ThenBy(grupo => grupo.Estado)
            .ToListAsync();

        return grupos
            .Select(grupo => new ReporteSolicitudPermiso(
                $"{grupo.Anio:D4}-{grupo.Mes:D2}",
                grupo.Estado.ToString(),
                grupo.Total))
            .ToList();
    }
}
