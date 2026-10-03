namespace GestorTareas.ControlAcceso;

public class Sesion
{
    public Guid Id { get; set; }

    public byte[] HashToken { get; set; } = [];

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public DateTime FechaEmision { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public bool Revocada { get; set; }
}
