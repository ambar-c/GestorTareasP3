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

public enum EstadoCambioEstadoUsuario
{
    Exitoso,
    UsuarioNoEncontrado,
    MismoUsuario
}

public sealed record ResultadoCambioEstadoUsuario(
    EstadoCambioEstadoUsuario Estado,
    string? Motivo,
    Usuario? Usuario);

public sealed class ServicioUsuarios(
    ContextoControlAcceso contexto,
    ServicioSesion servicioSesion)
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

    public async Task<ResultadoCambioEstadoUsuario> DesactivarAsync(
        Guid idAdministrador,
        Guid idUsuario)
    {
        Usuario? usuario = await contexto.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Id == idUsuario);

        if (usuario is null)
        {
            return new ResultadoCambioEstadoUsuario(
                EstadoCambioEstadoUsuario.UsuarioNoEncontrado,
                "Usuario no encontrado.",
                null);
        }

        if (idUsuario == idAdministrador)
        {
            return new ResultadoCambioEstadoUsuario(
                EstadoCambioEstadoUsuario.MismoUsuario,
                "No puedes desactivarte a ti mismo.",
                null);
        }

        usuario.Desactivado = true;
        await contexto.SaveChangesAsync();
        await servicioSesion.RevocarPorUsuarioAsync(idUsuario);

        return new ResultadoCambioEstadoUsuario(
            EstadoCambioEstadoUsuario.Exitoso,
            null,
            usuario);
    }

    public async Task<ResultadoCambioEstadoUsuario> ReactivarAsync(
        Guid idAdministrador,
        Guid idUsuario)
    {
        Usuario? usuario = await contexto.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Id == idUsuario);

        if (usuario is null)
        {
            return new ResultadoCambioEstadoUsuario(
                EstadoCambioEstadoUsuario.UsuarioNoEncontrado,
                "Usuario no encontrado.",
                null);
        }

        usuario.Desactivado = false;
        await contexto.SaveChangesAsync();

        return new ResultadoCambioEstadoUsuario(
            EstadoCambioEstadoUsuario.Exitoso,
            null,
            usuario);
    }
}
