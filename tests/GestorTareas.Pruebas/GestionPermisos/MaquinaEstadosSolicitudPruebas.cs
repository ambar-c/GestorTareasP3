using GestorTareas.GestionPermisos.Estados;

namespace GestorTareas.Pruebas.GestionPermisos;

public class MaquinaEstadosSolicitudPruebas
{
    [Theory]
    [InlineData(EstadoSolicitudPermiso.Pendiente, EstadoSolicitudPermiso.Aprobada)]
    [InlineData(EstadoSolicitudPermiso.Aprobada, EstadoSolicitudPermiso.Aplicada)]
    [InlineData(EstadoSolicitudPermiso.Pendiente, EstadoSolicitudPermiso.Rechazada)]
    public void EsTransicionValida_TransicionPermitida_DevuelveTrue(
        EstadoSolicitudPermiso estadoActual,
        EstadoSolicitudPermiso estadoDestino)
    {
        // Preparar
        // Los estados de la transición se preparan mediante los parámetros de InlineData.

        // Actuar
        var resultado = MaquinaEstadosSolicitud.EsTransicionValida(
            estadoActual,
            estadoDestino);

        // Verificar
        Assert.True(resultado);
    }

    [Theory]
    [InlineData(EstadoSolicitudPermiso.Rechazada, EstadoSolicitudPermiso.Aplicada)]
    [InlineData(EstadoSolicitudPermiso.Rechazada, EstadoSolicitudPermiso.Aprobada)]
    [InlineData(EstadoSolicitudPermiso.Aplicada, EstadoSolicitudPermiso.Pendiente)]
    [InlineData(EstadoSolicitudPermiso.Aprobada, EstadoSolicitudPermiso.Rechazada)]
    [InlineData(EstadoSolicitudPermiso.Pendiente, EstadoSolicitudPermiso.Aplicada)]
    [InlineData(EstadoSolicitudPermiso.Pendiente, EstadoSolicitudPermiso.Pendiente)]
    public void EsTransicionValida_TransicionProhibida_DevuelveFalse(
        EstadoSolicitudPermiso estadoActual,
        EstadoSolicitudPermiso estadoDestino)
    {
        // Preparar
        // Los estados de la transición se preparan mediante los parámetros de InlineData.

        // Actuar
        var resultado = MaquinaEstadosSolicitud.EsTransicionValida(
            estadoActual,
            estadoDestino);

        // Verificar
        Assert.False(resultado);
    }
}
