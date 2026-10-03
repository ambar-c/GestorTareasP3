namespace GestorTareas.ControlAcceso;

public enum EstadoRegistro
{
    Creado,
    DatosInvalidos,
    CorreoDuplicado,
    ErrorInterno
}

public sealed class ResultadoRegistro
{
    internal ResultadoRegistro(
        EstadoRegistro estado,
        IReadOnlyList<string> errores,
        Guid? idUsuario = null)
    {
        Estado = estado;
        Errores = errores;
        IdUsuario = idUsuario;
    }

    public EstadoRegistro Estado { get; }

    public IReadOnlyList<string> Errores { get; }

    public Guid? IdUsuario { get; }
}
