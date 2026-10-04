namespace GestorTareas.Negocio.Estados;

public record ResultadoTransicion(bool Exito, string? Error)
{
    public static ResultadoTransicion Ok() => new(true, null);

    public static ResultadoTransicion Rechazo(string mensaje) => new(false, mensaje);
}
