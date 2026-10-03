namespace GestorTareas.Notificaciones;

public interface IColaCorreos
{
    Task EncolarAsync(string destinatario, string asunto, string cuerpo);
}
