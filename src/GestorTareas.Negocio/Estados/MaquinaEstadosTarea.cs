using GestorTareas.Negocio.Entidades;

namespace GestorTareas.Negocio.Estados;

public static class MaquinaEstadosTarea
{
    private sealed record TransicionPermitida(
        EstadoTarea Desde,
        EstadoTarea Hacia,
        ActorTransicion QuienEjecuta);

    private sealed record TransicionProhibida(
        EstadoTarea Desde,
        EstadoTarea Hacia,
        string Motivo);

    private static readonly IReadOnlyList<TransicionPermitida> TransicionesPermitidas =
    [
        new(EstadoTarea.Pendiente, EstadoTarea.EnProgreso, ActorTransicion.Responsable),
        new(EstadoTarea.EnProgreso, EstadoTarea.Completada, ActorTransicion.Responsable),
        new(EstadoTarea.EnProgreso, EstadoTarea.Pendiente, ActorTransicion.Responsable),
        new(EstadoTarea.Pendiente, EstadoTarea.Cancelada, ActorTransicion.Administrador),
        new(EstadoTarea.EnProgreso, EstadoTarea.Cancelada, ActorTransicion.Administrador)
    ];

    private static readonly IReadOnlyList<TransicionProhibida> TransicionesProhibidas =
    [
        new(
            EstadoTarea.Pendiente,
            EstadoTarea.Completada,
            "Una tarea no se puede completar sin haberse empezado.")
    ];

    private static readonly IReadOnlySet<EstadoTarea> EstadosTerminales =
        new HashSet<EstadoTarea>
        {
            EstadoTarea.Completada,
            EstadoTarea.Cancelada
        };

    public static ResultadoTransicion Validar(
        Tarea tarea,
        EstadoTarea hacia,
        ActorTransicion actor,
        string? motivo)
    {
        if (EstadosTerminales.Contains(tarea.Estado))
        {
            return ResultadoTransicion.Rechazo(
                "La tarea está en un estado final y no puede cambiar.");
        }

        var transicionProhibida = TransicionesProhibidas.FirstOrDefault(t =>
            t.Desde == tarea.Estado && t.Hacia == hacia);

        if (transicionProhibida is not null)
        {
            return ResultadoTransicion.Rechazo(transicionProhibida.Motivo);
        }

        var transicion = TransicionesPermitidas.FirstOrDefault(t =>
            t.Desde == tarea.Estado && t.Hacia == hacia);

        if (transicion is null)
        {
            return ResultadoTransicion.Rechazo("Transición no permitida.");
        }

        if (transicion.QuienEjecuta != actor)
        {
            return ResultadoTransicion.Rechazo(
                "No tienes permiso para realizar esta transición.");
        }

        if (tarea.Estado == EstadoTarea.Pendiente &&
            hacia == EstadoTarea.EnProgreso &&
            tarea.UsuarioAsignadoId is null)
        {
            return ResultadoTransicion.Rechazo("La tarea no tiene responsable asignado.");
        }

        if (hacia == EstadoTarea.Cancelada && string.IsNullOrWhiteSpace(motivo))
        {
            return ResultadoTransicion.Rechazo(
                "Debes indicar el motivo de la cancelación.");
        }

        return ResultadoTransicion.Ok();
    }
}
