namespace GestorTareas.Negocio.Entidades;

public class Prioridad
{
    public Guid Id { get; set; }
    public required string Nombre { get; set; }
    public int Nivel { get; set; }
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
