using GestorTareas.Negocio.Entidades;
using GestorTareas.Negocio.Estados;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Negocio;

public enum EstadoOperacionTarea
{
    Exitoso,
    NoEncontrado,
    SinPermiso,
    DatosInvalidos,
    TransicionRechazada
}

public sealed record ResultadoOperacionTarea(
    EstadoOperacionTarea Estado,
    string? Error,
    Tarea? Tarea);

public sealed record ResultadoComentarTarea(
    EstadoOperacionTarea Estado,
    string? Error,
    Comentario? Comentario);

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

    public async Task<ResultadoComentarTarea> ComentarAsync(
        Guid idUsuario,
        bool esAdministrador,
        Guid idTarea,
        string? texto)
    {
        Tarea? tarea = await contexto.Tareas
            .SingleOrDefaultAsync(tarea => tarea.Id == idTarea);

        if (tarea is null)
        {
            return new ResultadoComentarTarea(
                EstadoOperacionTarea.NoEncontrado,
                "La tarea no existe.",
                null);
        }

        Proyecto? proyecto = await contexto.Proyectos
            .SingleOrDefaultAsync(proyecto => proyecto.Id == tarea.ProyectoId);

        if (proyecto is null)
        {
            return new ResultadoComentarTarea(
                EstadoOperacionTarea.NoEncontrado,
                "El proyecto no existe.",
                null);
        }

        if (!esAdministrador
            && proyecto.UsuarioCreadorId != idUsuario
            && tarea.UsuarioAsignadoId != idUsuario)
        {
            return new ResultadoComentarTarea(
                EstadoOperacionTarea.SinPermiso,
                "No tienes permiso para comentar esta tarea.",
                null);
        }

        string textoNormalizado = texto?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(textoNormalizado)
            || textoNormalizado.Length > 1000)
        {
            return new ResultadoComentarTarea(
                EstadoOperacionTarea.DatosInvalidos,
                "El comentario no es válido.",
                null);
        }

        var comentario = new Comentario
        {
            Id = Guid.NewGuid(),
            TareaId = idTarea,
            UsuarioId = idUsuario,
            Texto = textoNormalizado,
            FechaCreacion = DateTime.UtcNow
        };

        contexto.Comentarios.Add(comentario);
        await contexto.SaveChangesAsync();

        return new ResultadoComentarTarea(
            EstadoOperacionTarea.Exitoso,
            null,
            comentario);
    }

    public async Task<ResultadoOperacionTarea> AsignarResponsableAsync(
        Guid idUsuario,
        bool esAdministrador,
        Guid idTarea,
        Guid idResponsable)
    {
        Tarea? tarea = await contexto.Tareas
            .SingleOrDefaultAsync(tarea => tarea.Id == idTarea);

        if (tarea is null)
        {
            return new ResultadoOperacionTarea(
                EstadoOperacionTarea.NoEncontrado,
                "La tarea no existe.",
                null);
        }

        Proyecto? proyecto = await contexto.Proyectos
            .SingleOrDefaultAsync(proyecto => proyecto.Id == tarea.ProyectoId);

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
                "No tienes permiso para asignar responsables.",
                null);
        }

        if (idResponsable == Guid.Empty)
        {
            return new ResultadoOperacionTarea(
                EstadoOperacionTarea.DatosInvalidos,
                "El responsable no es válido.",
                null);
        }

        if (MaquinaEstadosTarea.EsTerminal(tarea.Estado))
        {
            return new ResultadoOperacionTarea(
                EstadoOperacionTarea.DatosInvalidos,
                "No se puede reasignar una tarea terminada.",
                null);
        }

        tarea.UsuarioAsignadoId = idResponsable;
        await contexto.SaveChangesAsync();

        return new ResultadoOperacionTarea(
            EstadoOperacionTarea.Exitoso,
            null,
            tarea);
    }

    public async Task<ResultadoOperacionTarea> CambiarEstadoAsync(
        Guid idUsuario,
        bool esAdministrador,
        Guid idTarea,
        EstadoTarea hacia,
        string? motivo)
    {
        Tarea? tarea = await contexto.Tareas
            .SingleOrDefaultAsync(tarea => tarea.Id == idTarea);

        if (tarea is null)
        {
            return new ResultadoOperacionTarea(
                EstadoOperacionTarea.NoEncontrado,
                "La tarea no existe.",
                null);
        }

        ActorTransicion actor;
        if (hacia == EstadoTarea.Cancelada && esAdministrador)
        {
            actor = ActorTransicion.Administrador;
        }
        else if (tarea.UsuarioAsignadoId == idUsuario)
        {
            actor = ActorTransicion.Responsable;
        }
        else if (esAdministrador)
        {
            actor = ActorTransicion.Administrador;
        }
        else
        {
            return new ResultadoOperacionTarea(
                EstadoOperacionTarea.SinPermiso,
                "No tienes permiso para cambiar el estado de la tarea.",
                null);
        }

        ResultadoTransicion resultado = tarea.CambiarEstado(hacia, actor, motivo);
        if (!resultado.Exito)
        {
            return new ResultadoOperacionTarea(
                EstadoOperacionTarea.TransicionRechazada,
                resultado.Error,
                null);
        }

        await contexto.SaveChangesAsync();

        return new ResultadoOperacionTarea(
            EstadoOperacionTarea.Exitoso,
            null,
            tarea);
    }
}
