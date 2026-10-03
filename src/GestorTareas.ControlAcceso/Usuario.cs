namespace GestorTareas.ControlAcceso;

public class Usuario
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public byte[] HashContrasena { get; set; } = [];

    public byte[] Sal { get; set; } = [];

    public Rol Rol { get; set; } = Rol.Estandar;

    public bool Activo { get; set; } = false;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
