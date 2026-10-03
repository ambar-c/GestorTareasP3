namespace GestorTareas.Notificaciones;

public class ColaCorreos(ContextoNotificaciones contexto) : IColaCorreos
{
    public async Task EncolarAsync(string destinatario, string asunto, string cuerpo)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            throw new ArgumentException("El destinatario no puede estar vacío.", nameof(destinatario));
        }

        if (destinatario.Length > 254)
        {
            throw new ArgumentException("El destinatario no puede exceder 254 caracteres.", nameof(destinatario));
        }

        if (string.IsNullOrWhiteSpace(asunto))
        {
            throw new ArgumentException("El asunto no puede estar vacío.", nameof(asunto));
        }

        if (asunto.Length > 200)
        {
            throw new ArgumentException("El asunto no puede exceder 200 caracteres.", nameof(asunto));
        }

        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            throw new ArgumentException("El cuerpo no puede estar vacío.", nameof(cuerpo));
        }

        var correo = new CorreoEnCola
        {
            Id = Guid.NewGuid(),
            Destinatario = destinatario,
            Asunto = asunto,
            Cuerpo = cuerpo,
            Estado = EstadoCorreo.Pendiente,
            Intentos = 0,
            FechaCreacion = DateTime.UtcNow
        };

        contexto.CorreosEnCola.Add(correo);
        await contexto.SaveChangesAsync();
    }
}
