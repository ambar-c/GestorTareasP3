namespace GestorTareas.ControlAcceso;

public enum EstadoReenvio
{
    Aceptado,
    DatosInvalidos,
    ErrorInterno
}

public sealed class ResultadoReenvio
{
    public ResultadoReenvio(
        EstadoReenvio estado,
        string mensaje,
        IReadOnlyList<string> errores)
    {
        Estado = estado;
        Mensaje = mensaje;
        Errores = errores;
    }

    public EstadoReenvio Estado { get; }

    public string Mensaje { get; }

    public IReadOnlyList<string> Errores { get; }
}
