using Microsoft.EntityFrameworkCore;

namespace GestorTareas.ControlAcceso;

public sealed class ServicioAutenticacion(
    ContextoControlAcceso contexto,
    ServicioSesion servicioSesion)
{
    private const int MaximoFallos = 5;
    private const int MinutosBloqueo = 15;
    private const string MensajeCredenciales = "El correo o la contraseña son incorrectos.";
    private const string MensajeBloqueo = "La cuenta está bloqueada temporalmente.";
    private const string MensajeInactiva = "La cuenta no está activa.";

    public async Task<ResultadoInicioSesion> IniciarSesionAsync(
        string? correo,
        string? contrasena)
    {
        ResultadoValidacionRegistro validacion = ValidadorRegistro.ValidarInicioSesion(correo, contrasena);
        if (!validacion.EsValido)
        {
            return new ResultadoInicioSesion(
                EstadoInicioSesion.DatosInvalidos,
                errores: validacion.Errores);
        }

        Usuario? usuario = await contexto.Usuarios
            .SingleOrDefaultAsync(usuario => usuario.Correo == validacion.CorreoNormalizado);

        if (usuario is null)
        {
            return CredencialesInvalidas();
        }

        DateTime ahora = DateTime.UtcNow;
        if (usuario.BloqueadoHasta > ahora)
        {
            return new ResultadoInicioSesion(
                EstadoInicioSesion.CuentaBloqueada,
                errores: [MensajeBloqueo]);
        }

        bool contrasenaCorrecta = HasheadorContrasena.Verificar(
            contrasena!,
            usuario.Sal,
            usuario.HashContrasena);

        if (!contrasenaCorrecta)
        {
            usuario.FallosInicioSesion++;
            if (usuario.FallosInicioSesion >= MaximoFallos)
            {
                usuario.BloqueadoHasta = ahora.AddMinutes(MinutosBloqueo);
            }

            await contexto.SaveChangesAsync();
            return usuario.BloqueadoHasta > ahora
                ? new ResultadoInicioSesion(EstadoInicioSesion.CuentaBloqueada, errores: [MensajeBloqueo])
                : CredencialesInvalidas();
        }

        if (!usuario.Activo)
        {
            return new ResultadoInicioSesion(
                EstadoInicioSesion.CuentaInactiva,
                errores: [MensajeInactiva]);
        }

        usuario.FallosInicioSesion = 0;
        usuario.BloqueadoHasta = null;
        await contexto.SaveChangesAsync();

        (string token, DateTime vencimiento) = await servicioSesion.CrearAsync(usuario.Id);
        return new ResultadoInicioSesion(
            EstadoInicioSesion.Exitoso,
            token,
            vencimiento);
    }

    public Task<bool> CerrarSesionAsync(string? token)
    {
        return servicioSesion.RevocarAsync(token);
    }

    private static ResultadoInicioSesion CredencialesInvalidas()
    {
        return new ResultadoInicioSesion(
            EstadoInicioSesion.CredencialesInvalidas,
            errores: [MensajeCredenciales]);
    }
}
