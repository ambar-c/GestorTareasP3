using GestorTareas.Negocio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Negocio;

public sealed record ReporteProgresoProyecto(
    Guid IdProyecto,
    string Nombre,
    int Total,
    int Pendientes,
    int EnProgreso,
    int Completadas,
    int Canceladas,
    decimal PorcentajeCompletado);

public sealed class ServicioReportesProgreso(NegocioDbContext contexto)
{
    public async Task<IReadOnlyList<ReporteProgresoProyecto>> ObtenerAsync(
        Guid idUsuario,
        bool esAdministrador,
        DateTime? desde,
        DateTime? hasta)
    {
        IQueryable<Proyecto> proyectos = contexto.Proyectos.AsNoTracking();

        if (!esAdministrador)
        {
            proyectos = proyectos.Where(proyecto => proyecto.UsuarioCreadorId == idUsuario);
        }

        IQueryable<Tarea> tareas = contexto.Tareas.AsNoTracking();

        if (desde.HasValue)
        {
            tareas = tareas.Where(tarea => tarea.FechaCreacion >= desde.Value);
        }

        if (hasta.HasValue)
        {
            tareas = tareas.Where(tarea => tarea.FechaCreacion < hasta.Value);
        }

        if (desde.HasValue || hasta.HasValue)
        {
            proyectos = proyectos.Where(proyecto => tareas.Any(tarea => tarea.ProyectoId == proyecto.Id));
        }

        var progreso = await proyectos
            .Select(proyecto => new
            {
                idProyecto = proyecto.Id,
                nombre = proyecto.Nombre,
                total = tareas.Count(tarea => tarea.ProyectoId == proyecto.Id),
                pendientes = tareas.Count(tarea =>
                    tarea.ProyectoId == proyecto.Id && tarea.Estado == EstadoTarea.Pendiente),
                enProgreso = tareas.Count(tarea =>
                    tarea.ProyectoId == proyecto.Id && tarea.Estado == EstadoTarea.EnProgreso),
                completadas = tareas.Count(tarea =>
                    tarea.ProyectoId == proyecto.Id && tarea.Estado == EstadoTarea.Completada),
                canceladas = tareas.Count(tarea =>
                    tarea.ProyectoId == proyecto.Id && tarea.Estado == EstadoTarea.Cancelada)
            })
            .OrderBy(proyecto => proyecto.nombre)
            .ToListAsync();

        return progreso
            .Select(proyecto => new ReporteProgresoProyecto(
                proyecto.idProyecto,
                proyecto.nombre,
                proyecto.total,
                proyecto.pendientes,
                proyecto.enProgreso,
                proyecto.completadas,
                proyecto.canceladas,
                ServicioProyectos.CalcularPorcentajeCompletado(
                    proyecto.completadas,
                    proyecto.total,
                    proyecto.canceladas)))
            .ToList();
    }
}
