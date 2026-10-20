using GestorTareas.GestionPermisos.Permisos;

namespace GestorTareas.GestionPermisos.Entidades;

public class PermisoOtorgado
{
    public Guid Id { get; set; }

    public Guid IdUsuario { get; set; }

    public Permiso Permiso { get; set; }

    public DateTime FechaOtorgamiento { get; set; } = DateTime.UtcNow;
}
