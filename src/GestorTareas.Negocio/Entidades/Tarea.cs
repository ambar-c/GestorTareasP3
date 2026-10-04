namespace GestorTareas.Negocio.Entidades;

public class Tarea
{
    public Guid Id { get; set; }
    public required string Titulo { get; set; }
    public Guid ProyectoId { get; set; }
    public Guid PrioridadId { get; set; }
    public Guid? UsuarioAsignadoId { get; set; }
    public EstadoTarea Estado { get; private set; } = EstadoTarea.Pendiente;
    public string? MotivoCancelacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
}
