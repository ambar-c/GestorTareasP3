using GestorTareas.Negocio.Entidades;
using GestorTareas.GestionPermisos;
using GestorTareas.GestionPermisos.Permisos;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Negocio;

public enum EstadoCrearProyecto
{
    Exitoso,
    SinPermiso,
    NombreInvalido
}

public sealed record ResultadoCrearProyecto(
    EstadoCrearProyecto Estado,
    string? Error,
    Proyecto? Proyecto)
{
    public bool Exito => Estado == EstadoCrearProyecto.Exitoso;
}

public enum EstadoConsultaProyecto
{
    Exitoso,
    NoEncontrado,
    SinPermiso
}

public sealed record ProgresoProyecto(
    Guid IdProyecto,
    string Nombre,
    int Total,
    int Pendientes,
    int EnProgreso,
    int Completadas,
    int Canceladas,
    int PorcentajeCompletado);

public sealed record ResultadoProgresoProyecto(
    EstadoConsultaProyecto Estado,
    string? Error,
    ProgresoProyecto? Progreso);

public sealed class ServicioProyectos(
    NegocioDbContext contexto,
    ServicioSolicitudesPermiso servicioSolicitudesPermiso)
{
    public async Task<ResultadoCrearProyecto> CrearAsync(
        Guid idUsuarioCreador,
        string? nombre,
        bool esAdministrador)
    {
        if (!esAdministrador
            && !await servicioSolicitudesPermiso.TienePermisoAsync(
                idUsuarioCreador,
                Permiso.CrearProyecto))
        {
            return new ResultadoCrearProyecto(
                EstadoCrearProyecto.SinPermiso,
                "No tienes permiso para crear proyectos.",
                null);
        }

        string? nombreNormalizado = nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombreNormalizado)
            || nombreNormalizado.Length > 100)
        {
            return new ResultadoCrearProyecto(
                EstadoCrearProyecto.NombreInvalido,
                "El nombre del proyecto no es válido.",
                null);
        }

        var proyecto = new Proyecto
        {
            Id = Guid.NewGuid(),
            Nombre = nombreNormalizado,
            UsuarioCreadorId = idUsuarioCreador,
            FechaCreacion = DateTime.UtcNow
        };

        contexto.Proyectos.Add(proyecto);
        await contexto.SaveChangesAsync();

        return new ResultadoCrearProyecto(EstadoCrearProyecto.Exitoso, null, proyecto);
    }

    public async Task<ResultadoProgresoProyecto> ObtenerProgresoAsync(
        Guid idUsuario,
        bool esAdministrador,
        Guid idProyecto)
    {
        Proyecto? proyecto = await contexto.Proyectos
            .AsNoTracking()
            .SingleOrDefaultAsync(proyecto => proyecto.Id == idProyecto);

        if (proyecto is null)
        {
            return new ResultadoProgresoProyecto(
                EstadoConsultaProyecto.NoEncontrado,
                "El proyecto no existe.",
                null);
        }

        if (!esAdministrador && proyecto.UsuarioCreadorId != idUsuario)
        {
            return new ResultadoProgresoProyecto(
                EstadoConsultaProyecto.SinPermiso,
                "No tienes permiso para consultar el progreso del proyecto.",
                null);
        }

        List<EstadoTarea> estados = await contexto.Tareas
            .AsNoTracking()
            .Where(tarea => tarea.ProyectoId == idProyecto)
            .Select(tarea => tarea.Estado)
            .ToListAsync();

        int total = estados.Count;
        int pendientes = estados.Count(estado => estado == EstadoTarea.Pendiente);
        int enProgreso = estados.Count(estado => estado == EstadoTarea.EnProgreso);
        int completadas = estados.Count(estado => estado == EstadoTarea.Completada);
        int canceladas = estados.Count(estado => estado == EstadoTarea.Cancelada);
        int totalSinCanceladas = total - canceladas;
        int porcentajeCompletado = totalSinCanceladas == 0
            ? 0
            : completadas * 100 / totalSinCanceladas;

        return new ResultadoProgresoProyecto(
            EstadoConsultaProyecto.Exitoso,
            null,
            new ProgresoProyecto(
                proyecto.Id,
                proyecto.Nombre,
                total,
                pendientes,
                enProgreso,
                completadas,
                canceladas,
                porcentajeCompletado));
    }
}
