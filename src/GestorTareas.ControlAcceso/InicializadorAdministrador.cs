using Microsoft.EntityFrameworkCore;

namespace GestorTareas.ControlAcceso;

public enum EstadoInicializacionAdministrador
{
    Creado,
    YaExistia,
    DatosInvalidos
}

public sealed record ResultadoInicializacionAdministrador(
    EstadoInicializacionAdministrador Estado,
    IReadOnlyList<string> Errores);

public sealed class InicializadorAdministrador(ContextoControlAcceso contexto)
{
    public async Task<ResultadoInicializacionAdministrador> CrearAsync(
        string nombre,
        string correo,
        string contrasena)
    {
        ResultadoValidacionRegistro validacion = ValidadorRegistro.Validar(
            nombre,
            correo,
            contrasena);

        bool correoYaRegistrado = await contexto.Usuarios.AnyAsync(
            usuario => usuario.Correo == validacion.CorreoNormalizado);

        if (correoYaRegistrado)
        {
            return new ResultadoInicializacionAdministrador(
                EstadoInicializacionAdministrador.YaExistia,
                Array.Empty<string>());
        }

        if (!validacion.EsValido)
        {
            return new ResultadoInicializacionAdministrador(
                EstadoInicializacionAdministrador.DatosInvalidos,
                validacion.Errores);
        }

        (byte[] sal, byte[] hash) = HasheadorContrasena.Hashear(contrasena);
        contexto.Usuarios.Add(new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = nombre.Trim(),
            Correo = validacion.CorreoNormalizado,
            HashContrasena = hash,
            Sal = sal,
            Rol = Rol.Administrador,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });

        await contexto.SaveChangesAsync();

        return new ResultadoInicializacionAdministrador(
            EstadoInicializacionAdministrador.Creado,
            Array.Empty<string>());
    }
}
