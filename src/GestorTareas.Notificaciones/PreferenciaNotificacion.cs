namespace GestorTareas.Notificaciones;

public class PreferenciaNotificacion
{
    public Guid UsuarioId { get; set; }
    public bool CorreoActivo { get; set; } = true;
}
