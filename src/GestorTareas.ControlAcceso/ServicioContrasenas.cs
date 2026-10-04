using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace GestorTareas.ControlAcceso;

public sealed record ResultadoEstablecerContrasena(
    bool Exitoso,
    IReadOnlyList<string> Errores);

public sealed class ServicioContrasenas(
    ContextoControlAcceso contexto,
    ServicioSesion servicioSesion)
{
    public async Task<ResultadoEstablecerContrasena> EstablecerContrasenaAsync(
        Usuario usuario,
        string? contrasenaNueva)
    {
        IReadOnlyList<string> errores = ValidadorRegistro.ValidarContrasena(contrasenaNueva);
        if (errores.Count > 0)
        {
            return new ResultadoEstablecerContrasena(false, errores);
        }

        (byte[] sal, byte[] hash) = HasheadorContrasena.Hashear(contrasenaNueva!);
        usuario.Sal = sal;
        usuario.HashContrasena = hash;
        usuario.FallosInicioSesion = 0;
        usuario.BloqueadoHasta = null;

        await servicioSesion.RevocarPorUsuarioAsync(usuario.Id);
        await contexto.SaveChangesAsync();

        return new ResultadoEstablecerContrasena(true, Array.Empty<string>());
    }

    public async Task<ResultadoEstablecerContrasena> CambiarAsync(
        Guid idUsuario,
        string? contrasenaActual,
        string? contrasenaNueva)
    {
        Usuario? usuario = await contexto.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Id == idUsuario);

        bool contrasenaCorrecta = usuario is not null
            && contrasenaActual is not null
            && HasheadorContrasena.Verificar(
                contrasenaActual,
                usuario.Sal,
                usuario.HashContrasena);

        if (!contrasenaCorrecta)
        {
            return new ResultadoEstablecerContrasena(
                false,
                ["La contraseña actual no es correcta."]);
        }

        return await EstablecerContrasenaAsync(usuario!, contrasenaNueva);
    }

    public async Task InvalidarContrasenaAsync(Usuario usuario)
    {
        string secretoAleatorio = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        (byte[] sal, byte[] hash) = HasheadorContrasena.Hashear(secretoAleatorio);

        usuario.Sal = sal;
        usuario.HashContrasena = hash;

        await servicioSesion.RevocarPorUsuarioAsync(usuario.Id);
        await contexto.SaveChangesAsync();
    }
}
