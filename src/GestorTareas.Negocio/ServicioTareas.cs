using GestorTareas.Negocio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Negocio;

public enum EstadoOperacionTarea
{
    Exitoso,
    NoEncontrado,
    SinPermiso,
    DatosInvalidos
}

public sealed record ResultadoOperacionTarea(
    EstadoOperacionTarea Estado,
    string? Error,
    Tarea? Tarea);

public sealed class ServicioTareas(NegocioDbContext contexto)
{
    public async Task<ResultadoOperacionTarea> CrearAsync(
        Guid idUsuario,
        bool esAdministrador,
        Guid idProyecto,
        string? titulo,
        Guid idPrioridad)
    {
        Proyecto? proyecto = await contexto.Proyectos
            .SingleOrDefaultAsync(proyecto => proyecto.Id == idProyecto);

        if (proyecto is null)
        {
            return new ResultadoOperacionTarea(
                EstadoOperacionTarea.NoEncontrado,
                "El proyecto no existe.",
                null);
        }

        if (!esAdministrador && proyecto.UsuarioCreadorId != idUsuario)
        {
            return new ResultadoOperacionTarea(
                EstadoOperacionTarea.SinPermiso,
                "No tienes permiso para crear tareas en este proyecto.",
                null);
        }

        string tituloNormalizado = titulo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(tituloNormalizado)
            || tituloNormalizado.Length > 150)
        {
            return new ResultadoOperacionTarea(
                EstadoOperacionTarea.DatosInvalidos,
                "El título no es válido.",
                null);
        }

        bool prioridadExiste = await contexto.Prioridades
            .AnyAsync(prioridad => prioridad.Id == idPrioridad);
        if (!prioridadExiste)
        {
            return new ResultadoOperacionTarea(
                EstadoOperacionTarea.DatosInvalidos,
                "La prioridad no existe.",
                null);
        }

        var tarea = new Tarea
        {
            Id = Guid.NewGuid(),
            Titulo = tituloNormalizado,
            ProyectoId = idProyecto,
            PrioridadId = idPrioridad,
            FechaCreacion = DateTime.UtcNow
        };

        contexto.Tareas.Add(tarea);
        await contexto.SaveChangesAsync();

        return new ResultadoOperacionTarea(
            EstadoOperacionTarea.Exitoso,
            null,
            tarea);
    }
}
