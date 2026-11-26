using System.Net.Mail;

namespace GestorTareas.Notificaciones;

public static class ValidadorCorreo
{
    public static bool EsValido(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            return false;
        }

        string correoRecortado = correo.Trim();
        if (correoRecortado.Length > 254)
        {
            return false;
        }

        return MailAddress.TryCreate(correoRecortado, out MailAddress? direccion)
            && direccion.Address == correoRecortado;
    }
}
