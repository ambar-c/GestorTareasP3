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
}
