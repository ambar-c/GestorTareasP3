using Microsoft.EntityFrameworkCore;

namespace GestorTareas.ControlAcceso;

public enum EstadoCambioRol
{
    Exitoso,
    UsuarioNoEncontrado,
    MismoUsuario
}

public sealed record ResultadoCambioRol(
    EstadoCambioRol Estado,
    string? Motivo,
    Usuario? Usuario);

public sealed class ServicioUsuarios(ContextoControlAcceso contexto)
{
    public async Task<ResultadoCambioRol> CambiarRolAsync(
        Guid idAdministrador,
        Guid idUsuario,
        Rol nuevoRol)
    {
        Usuario? usuario = await contexto.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Id == idUsuario);

        if (usuario is null)
        {
            return new ResultadoCambioRol(
                EstadoCambioRol.UsuarioNoEncontrado,
                "Usuario no encontrado.",
                null);
        }

        if (idUsuario == idAdministrador)
        {
            return new ResultadoCambioRol(
                EstadoCambioRol.MismoUsuario,
                "No puedes cambiar tu propio rol.",
                null);
        }

        usuario.Rol = nuevoRol;
        await contexto.SaveChangesAsync();

        return new ResultadoCambioRol(
            EstadoCambioRol.Exitoso,
            null,
            usuario);
    }
}
