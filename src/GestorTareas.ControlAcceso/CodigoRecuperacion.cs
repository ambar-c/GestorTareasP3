namespace GestorTareas.ControlAcceso;

public class CodigoRecuperacion
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    public byte[] HashCodigo { get; set; } = [];

    public DateTime FechaEmision { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public bool Usado { get; set; }
}
