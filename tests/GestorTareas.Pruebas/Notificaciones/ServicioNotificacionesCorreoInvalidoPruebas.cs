using GestorTareas.Notificaciones;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Pruebas.Notificaciones;

public class ServicioNotificacionesCorreoInvalidoPruebas
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("abc")]
    [InlineData("sin-arroba.com")]
    public async Task H2_NotificarAsync_con_correo_invalido_conserva_la_notificacion_y_no_encola(
        string correo)
    {
        // H2: un correo inválido no debe tumbar la operación ni encolarse.
        // Preparar
        Guid usuarioId = Guid.NewGuid();
        using var contexto = CrearContexto();
        var colaCorreos = new ColaCorreos(contexto);
        var servicio = new ServicioNotificaciones(contexto, colaCorreos);

        // Actuar
        Exception? excepcion = await Record.ExceptionAsync(() => servicio.NotificarAsync(
            usuarioId,
            correo,
            "Título de prueba",
            "Mensaje de prueba"));

        // Verificar
        Assert.Null(excepcion);
        Assert.Equal(1, await contexto.Notificaciones.CountAsync(notificacion =>
            notificacion.UsuarioId == usuarioId));
        Assert.Equal(0, await contexto.CorreosEnCola.CountAsync());
    }

    [Fact]
    public async Task H2_NotificarAsync_con_correo_valido_encola_una_notificacion()
    {
        // H2: un correo válido debe conservar el comportamiento de encolado.
        // Preparar
        Guid usuarioId = Guid.NewGuid();
        using var contexto = CrearContexto();
        var colaCorreos = new ColaCorreos(contexto);
        var servicio = new ServicioNotificaciones(contexto, colaCorreos);

        // Actuar
        await servicio.NotificarAsync(
            usuarioId,
            "ambar@correo.com",
            "Título de prueba",
            "Mensaje de prueba");

        // Verificar
        Assert.Equal(1, await contexto.Notificaciones.CountAsync(notificacion =>
            notificacion.UsuarioId == usuarioId));
        Assert.Equal(1, await contexto.CorreosEnCola.CountAsync());
    }

    private static ContextoNotificaciones CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoNotificaciones>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ContextoNotificaciones(opciones);
    }
}
