using GestorTareas.Notificaciones;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Pruebas.Notificaciones;

public class ColaCorreosFormatoPruebas
{
    [Theory]
    [InlineData("abc")]
    [InlineData("sin-arroba.com")]
    [InlineData("a@")] 
    [InlineData("Ana ana@correo.com")]
    public async Task H3_EncolarAsync_rechaza_destinatario_con_formato_invalido(
        string destinatario)
    {
        // H3: la cola no debe aceptar destinatarios con formato inválido.
        // Preparar
        using var contexto = CrearContexto();
        var colaCorreos = new ColaCorreos(contexto);

        // Actuar
        await Assert.ThrowsAsync<ArgumentException>(() => colaCorreos.EncolarAsync(
            destinatario,
            "Asunto de prueba",
            "Cuerpo de prueba"));

        // Verificar
        Assert.Equal(0, await contexto.CorreosEnCola.CountAsync());
    }

    [Fact]
    public async Task H3_EncolarAsync_con_destinatario_valido_encola_un_correo()
    {
        // H3: un destinatario válido debe conservar el comportamiento de encolado.
        // Preparar
        using var contexto = CrearContexto();
        var colaCorreos = new ColaCorreos(contexto);

        // Actuar
        await colaCorreos.EncolarAsync(
            "ambar@correo.com",
            "Asunto de prueba",
            "Cuerpo de prueba");

        // Verificar
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
