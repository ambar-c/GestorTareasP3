using GestorTareas.Negocio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Negocio;

public sealed record ResultadoCrearProyecto(
    bool Exito,
    string? Error,
    Proyecto? Proyecto);

public sealed class ServicioProyectos(NegocioDbContext contexto)
{
    public async Task<ResultadoCrearProyecto> CrearAsync(
        Guid idUsuarioCreador,
        string? nombre)
    {
        string? nombreNormalizado = nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombreNormalizado)
            || nombreNormalizado.Length > 100)
        {
            return new ResultadoCrearProyecto(
                false,
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

        return new ResultadoCrearProyecto(true, null, proyecto);
    }
}
