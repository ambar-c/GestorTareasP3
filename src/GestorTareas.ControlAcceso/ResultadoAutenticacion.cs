namespace GestorTareas.ControlAcceso;

public enum EstadoInicioSesion
{
    Exitoso,
    DatosInvalidos,
    CredencialesInvalidas,
    CuentaBloqueada,
    CuentaInactiva,
    CuentaDesactivada
}

public sealed class ResultadoInicioSesion
{
    internal ResultadoInicioSesion(
        EstadoInicioSesion estado,
        string? token = null,
        DateTime? vencimiento = null,
        IReadOnlyList<string>? errores = null)
    {
        Estado = estado;
        Token = token;
        Vencimiento = vencimiento;
        Errores = errores ?? Array.Empty<string>();
    }

    public EstadoInicioSesion Estado { get; }

    public string? Token { get; }

    public DateTime? Vencimiento { get; }

    public IReadOnlyList<string> Errores { get; }
}
