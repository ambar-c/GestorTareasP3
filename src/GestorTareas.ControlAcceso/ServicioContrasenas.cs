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
}
