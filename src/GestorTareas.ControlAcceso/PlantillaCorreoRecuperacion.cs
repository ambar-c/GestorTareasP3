namespace GestorTareas.ControlAcceso;

public static class PlantillaCorreoRecuperacion
{
    public static (string Asunto, string Cuerpo) Construir(
        string nombre,
        string codigo)
    {
        string cuerpo = $"Hola, {nombre}:\n\n"
            + "Solicitaste recuperar tu contraseña en Gestor de Tareas.\n\n"
            + $"Tu código de recuperación es: {codigo}\n\n"
            + "El código vence en 30 minutos.\n\n"
            + "Si no solicitaste esta recuperación, puedes ignorar este correo.";

        return ("Recuperación de contraseña", cuerpo);
    }
}
