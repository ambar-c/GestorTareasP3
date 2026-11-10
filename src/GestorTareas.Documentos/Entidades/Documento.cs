namespace GestorTareas.Documentos.Entidades;

public class Documento
{
    public Guid Id { get; set; }

    public string NombreOriginal { get; set; } = string.Empty;

    public string TipoContenido { get; set; } = string.Empty;

    public long TamanoBytes { get; set; }

    public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

    public Guid IdUsuarioPropietario { get; set; }

    public string NombreAlmacenado { get; set; } = string.Empty;

    public bool Eliminado { get; set; }
}
