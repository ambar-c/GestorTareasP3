namespace GestorTareas.ControlAcceso;

public class Usuario
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public byte[] HashContrasena { get; set; } = [];

    public byte[] Sal { get; set; } = [];

    public byte[]? HashTokenActivacion { get; set; }

    public DateTime? VencimientoActivacion { get; set; }

    public Rol Rol { get; set; } = Rol.Estandar;

    public bool Activo { get; set; } = false;

    public bool Desactivado { get; set; } = false;

    public int FallosInicioSesion { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
