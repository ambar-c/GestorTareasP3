namespace GestorTareas.GestionPermisos.Estados;

public static class MaquinaEstadosSolicitud
{
    private static readonly IReadOnlySet<(
        EstadoSolicitudPermiso EstadoActual,
        EstadoSolicitudPermiso EstadoDestino)> TransicionesPermitidas =
        new HashSet<(
            EstadoSolicitudPermiso EstadoActual,
            EstadoSolicitudPermiso EstadoDestino)>
        {
            (EstadoSolicitudPermiso.Pendiente, EstadoSolicitudPermiso.Aprobada),
            (EstadoSolicitudPermiso.Aprobada, EstadoSolicitudPermiso.Aplicada),
            (EstadoSolicitudPermiso.Pendiente, EstadoSolicitudPermiso.Rechazada),
            (EstadoSolicitudPermiso.Aprobada, EstadoSolicitudPermiso.Pendiente)
        };

    public static bool EsTransicionValida(
        EstadoSolicitudPermiso estadoActual,
        EstadoSolicitudPermiso estadoDestino) =>
        TransicionesPermitidas.Contains((estadoActual, estadoDestino));
}
