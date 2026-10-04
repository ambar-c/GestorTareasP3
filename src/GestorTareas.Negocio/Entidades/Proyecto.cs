namespace GestorTareas.Negocio.Entidades;

public class Proyecto
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public Guid UsuarioCreadorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
