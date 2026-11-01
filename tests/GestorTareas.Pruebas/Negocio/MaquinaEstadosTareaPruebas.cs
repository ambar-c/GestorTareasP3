using GestorTareas.Negocio.Entidades;
using GestorTareas.Negocio.Estados;

namespace GestorTareas.Pruebas.Negocio;

public class MaquinaEstadosTareaPruebas
{
    [Fact]
    public void P1_PendienteAEnProgreso_ResponsableConTareaAsignada_CambiaAEnProgreso()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();

        // Actuar
        var resultado = tarea.CambiarEstado(EstadoTarea.EnProgreso, ActorTransicion.Responsable);

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarea.EnProgreso, tarea.Estado);
    }

    [Fact]
    public void P2_EnProgresoACompletada_Responsable_CambiaACompletada()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();
        tarea.CambiarEstado(EstadoTarea.EnProgreso, ActorTransicion.Responsable);

        // Actuar
        var resultado = tarea.CambiarEstado(EstadoTarea.Completada, ActorTransicion.Responsable);

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarea.Completada, tarea.Estado);
    }

    [Fact]
    public void P3_EnProgresoAPendiente_Responsable_CambiaAPendiente()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();
        tarea.CambiarEstado(EstadoTarea.EnProgreso, ActorTransicion.Responsable);

        // Actuar
        var resultado = tarea.CambiarEstado(EstadoTarea.Pendiente, ActorTransicion.Responsable);

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarea.Pendiente, tarea.Estado);
    }

    [Fact]
    public void P4_PendienteACancelada_AdministradorConMotivo_CambiaACanceladaYGuardaMotivo()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();

        // Actuar
        var resultado = tarea.CambiarEstado(
            EstadoTarea.Cancelada,
            ActorTransicion.Administrador,
            "Se canceló por decisión del proyecto");

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarea.Cancelada, tarea.Estado);
        Assert.Equal("Se canceló por decisión del proyecto", tarea.MotivoCancelacion);
    }

    [Fact]
    public void P5_EnProgresoACancelada_AdministradorConMotivo_CambiaACancelada()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();
        tarea.CambiarEstado(EstadoTarea.EnProgreso, ActorTransicion.Responsable);

        // Actuar
        var resultado = tarea.CambiarEstado(
            EstadoTarea.Cancelada,
            ActorTransicion.Administrador,
            "Se canceló por decisión del proyecto");

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarea.Cancelada, tarea.Estado);
    }

    [Fact]
    public void X1_PendienteACompletada_ResponsableConTareaAsignada_SeRechazaYSigueEnPendiente()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();

        // Actuar
        var resultado = tarea.CambiarEstado(EstadoTarea.Completada, ActorTransicion.Responsable);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarea.Pendiente, tarea.Estado);
    }

    [Fact]
    public void X2_PendienteAPendiente_SeRechazaYNoCambia()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();

        // Actuar
        var resultado = tarea.CambiarEstado(EstadoTarea.Pendiente, ActorTransicion.Responsable);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarea.Pendiente, tarea.Estado);
    }

    [Fact]
    public void X3_EnProgresoAEnProgreso_SeRechazaYNoCambia()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();
        tarea.CambiarEstado(EstadoTarea.EnProgreso, ActorTransicion.Responsable);

        // Actuar
        var resultado = tarea.CambiarEstado(EstadoTarea.EnProgreso, ActorTransicion.Responsable);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarea.EnProgreso, tarea.Estado);
    }

    [Theory]
    [InlineData(EstadoTarea.Pendiente, ActorTransicion.Responsable)]
    [InlineData(EstadoTarea.Pendiente, ActorTransicion.Administrador)]
    [InlineData(EstadoTarea.EnProgreso, ActorTransicion.Responsable)]
    [InlineData(EstadoTarea.EnProgreso, ActorTransicion.Administrador)]
    [InlineData(EstadoTarea.Completada, ActorTransicion.Responsable)]
    [InlineData(EstadoTarea.Completada, ActorTransicion.Administrador)]
    [InlineData(EstadoTarea.Cancelada, ActorTransicion.Responsable)]
    [InlineData(EstadoTarea.Cancelada, ActorTransicion.Administrador)]
    public void X4_DesdeCompletada_CualquierDestinoYActor_SeRechazaYSigueCompletada(
        EstadoTarea destino,
        ActorTransicion actor)
    {
        // Preparar
        var tarea = CrearTareaConResponsable();
        tarea.CambiarEstado(EstadoTarea.EnProgreso, ActorTransicion.Responsable);
        tarea.CambiarEstado(EstadoTarea.Completada, ActorTransicion.Responsable);

        // Actuar
        var resultado = tarea.CambiarEstado(destino, actor, "Motivo escrito");

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarea.Completada, tarea.Estado);
    }

    [Theory]
    [InlineData(EstadoTarea.Pendiente, ActorTransicion.Responsable)]
    [InlineData(EstadoTarea.Pendiente, ActorTransicion.Administrador)]
    [InlineData(EstadoTarea.EnProgreso, ActorTransicion.Responsable)]
    [InlineData(EstadoTarea.EnProgreso, ActorTransicion.Administrador)]
    [InlineData(EstadoTarea.Completada, ActorTransicion.Responsable)]
    [InlineData(EstadoTarea.Completada, ActorTransicion.Administrador)]
    [InlineData(EstadoTarea.Cancelada, ActorTransicion.Responsable)]
    [InlineData(EstadoTarea.Cancelada, ActorTransicion.Administrador)]
    public void X5_DesdeCancelada_CualquierDestinoYActor_SeRechazaYSigueCancelada(
        EstadoTarea destino,
        ActorTransicion actor)
    {
        // Preparar
        var tarea = CrearTareaConResponsable();
        tarea.CambiarEstado(EstadoTarea.Cancelada, ActorTransicion.Administrador, "Motivo escrito");

        // Actuar
        var resultado = tarea.CambiarEstado(destino, actor, "Motivo escrito");

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarea.Cancelada, tarea.Estado);
    }

    [Theory]
    [InlineData(EstadoTarea.Pendiente, false)]
    [InlineData(EstadoTarea.EnProgreso, false)]
    [InlineData(EstadoTarea.Completada, true)]
    [InlineData(EstadoTarea.Cancelada, true)]
    public void EstadoTerminal_EsTerminal_SoloCompletadaYCancelada(
        EstadoTarea estado,
        bool esTerminalEsperado)
    {
        // Preparar: el estado a consultar llega por InlineData

        // Actuar
        var resultado = MaquinaEstadosTarea.EsTerminal(estado);

        // Verificar
        Assert.Equal(esTerminalEsperado, resultado);
    }

    [Fact]
    public void C1_PendienteAEnProgreso_SinResponsableAsignado_SeRechaza()
    {
        // Preparar
        var tarea = CrearTareaSinResponsable();

        // Actuar
        var resultado = tarea.CambiarEstado(EstadoTarea.EnProgreso, ActorTransicion.Responsable);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarea.Pendiente, tarea.Estado);
    }

    [Fact]
    public void C2_PendienteAEnProgreso_Administrador_SeRechaza()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();

        // Actuar
        var resultado = tarea.CambiarEstado(EstadoTarea.EnProgreso, ActorTransicion.Administrador);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarea.Pendiente, tarea.Estado);
    }

    [Fact]
    public void C3_PendienteACancelada_SinMotivo_SeRechaza()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();

        // Actuar
        var resultado = tarea.CambiarEstado(EstadoTarea.Cancelada, ActorTransicion.Administrador, "   ");

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarea.Pendiente, tarea.Estado);
    }

    [Fact]
    public void C4_EnProgresoACancelada_Responsable_SeRechaza()
    {
        // Preparar
        var tarea = CrearTareaConResponsable();
        tarea.CambiarEstado(EstadoTarea.EnProgreso, ActorTransicion.Responsable);

        // Actuar
        var resultado = tarea.CambiarEstado(EstadoTarea.Cancelada, ActorTransicion.Responsable, "Motivo escrito");

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarea.EnProgreso, tarea.Estado);
    }

    private static Tarea CrearTareaConResponsable()
    {
        return new Tarea
        {
            Titulo = "Tarea de prueba",
            UsuarioAsignadoId = Guid.NewGuid()
        };
    }

    private static Tarea CrearTareaSinResponsable()
    {
        return new Tarea
        {
            Titulo = "Tarea de prueba"
        };
    }
}
