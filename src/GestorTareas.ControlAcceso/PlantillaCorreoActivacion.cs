namespace GestorTareas.ControlAcceso;

public static class PlantillaCorreoActivacion
{
    public static (string Asunto, string Cuerpo) Construir(
        string nombre,
        string urlBase,
        string token)
    {
        string enlace = $"{urlBase.TrimEnd('/')}/api/activar?token={Uri.EscapeDataString(token)}";
        string cuerpo = $"Hola, {nombre}:\n\n"
            + "Te registraste en Gestor de Tareas.\n\n"
            + $"Activa tu cuenta mediante este enlace:\n{enlace}\n\n"
            + "El enlace vence en 24 horas y solo puede usarse una vez.\n\n"
            + "Si no creaste esta cuenta, ignora este mensaje.";

        return ("Activa tu cuenta en Gestor de Tareas", cuerpo);
    }
}
