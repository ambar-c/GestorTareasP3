using GestorTareas.GestionPermisos.Estados;
using GestorTareas.GestionPermisos.Permisos;

namespace GestorTareas.GestionPermisos.Entidades;

public class SolicitudPermiso
{
    public Guid Id { get; set; }

    public Guid IdSolicitante { get; set; }

    public Permiso PermisoSolicitado { get; set; }

    public EstadoSolicitudPermiso Estado { get; set; } = EstadoSolicitudPermiso.Pendiente;

    public Guid? IdAprobador { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public DateTime? FechaResolucion { get; set; }

    public string? MotivoRechazo { get; set; }
}
