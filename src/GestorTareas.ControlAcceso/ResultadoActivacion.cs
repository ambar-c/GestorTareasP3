namespace GestorTareas.ControlAcceso;

public enum EstadoActivacion
{
    Activada,
    EnlaceInvalido,
    ErrorInterno
}

public sealed class ResultadoActivacion
{
    public ResultadoActivacion(EstadoActivacion estado, string mensaje)
    {
        Estado = estado;
        Mensaje = mensaje;
    }

    public EstadoActivacion Estado { get; }

    public string Mensaje { get; }
}
