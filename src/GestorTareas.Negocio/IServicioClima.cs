namespace GestorTareas.Negocio;

public interface IServicioClima
{
    Task<ResultadoClima> ObtenerClimaActualAsync(string ciudad);
}

public enum EstadoClima
{
    Exitoso,
    SinClave,
    CiudadNoEncontrada,
    ApiNoDisponible
}

public sealed record ClimaActual(
    decimal TemperaturaCelsius,
    int Humedad,
    string DescripcionCondicion,
    string Ciudad);

public sealed record ResultadoClima(
    EstadoClima Estado,
    ClimaActual? Clima);
