namespace GestorTareas.Negocio.Entidades;

public class Comentario
{
    public Guid Id { get; set; }
    public Guid TareaId { get; set; }
    public Guid UsuarioId { get; set; }
    public required string Texto { get; set; }
    public DateTime FechaCreacion { get; set; }
}
