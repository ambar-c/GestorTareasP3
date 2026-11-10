using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestorTareas.Negocio;

public sealed class ServicioClima(
    HttpClient cliente,
    OpcionesClima opciones) : IServicioClima
{
    private const string UrlBase = "https://api.weatherapi.com/v1/current.json";

    public async Task<ResultadoClima> ObtenerClimaActualAsync(string ciudad)
    {
        string? apiKey = opciones.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ResultadoClima(EstadoClima.SinClave, null);
        }

        string url = $"{UrlBase}?key={Uri.EscapeDataString(apiKey)}"
            + $"&q={Uri.EscapeDataString(ciudad)}&lang=es";

        try
        {
            using HttpResponseMessage respuesta = await cliente.GetAsync(url);
            string contenido = await respuesta.Content.ReadAsStringAsync();

            if (respuesta.StatusCode == HttpStatusCode.BadRequest)
            {
                RespuestaWeatherApi? error = JsonSerializer.Deserialize<RespuestaWeatherApi>(contenido);
                if (error?.Error?.Codigo == 1006)
                {
                    return new ResultadoClima(EstadoClima.CiudadNoEncontrada, null);
                }

                return new ResultadoClima(EstadoClima.ApiNoDisponible, null);
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                return new ResultadoClima(EstadoClima.ApiNoDisponible, null);
            }

            RespuestaWeatherApi? datos = JsonSerializer.Deserialize<RespuestaWeatherApi>(contenido);
            if (datos?.Location is null
                || datos.Current is null
                || datos.Current.Condition is null
                || string.IsNullOrWhiteSpace(datos.Location.Name)
                || string.IsNullOrWhiteSpace(datos.Current.Condition.Text))
            {
                return new ResultadoClima(EstadoClima.ApiNoDisponible, null);
            }

            return new ResultadoClima(
                EstadoClima.Exitoso,
                new ClimaActual(
                    datos.Current.TemperaturaCelsius,
                    datos.Current.Humedad,
                    datos.Current.Condition.Text,
                    datos.Location.Name));
        }
        catch (Exception)
        {
            return new ResultadoClima(EstadoClima.ApiNoDisponible, null);
        }
    }

    private sealed record RespuestaWeatherApi(
        [property: JsonPropertyName("location")] RespuestaUbicacion? Location,
        [property: JsonPropertyName("current")] RespuestaActual? Current,
        [property: JsonPropertyName("error")] RespuestaError? Error);

    private sealed record RespuestaUbicacion(
        [property: JsonPropertyName("name")] string? Name);

    private sealed record RespuestaActual(
        [property: JsonPropertyName("temp_c")] decimal TemperaturaCelsius,
        [property: JsonPropertyName("humidity")] int Humedad,
        [property: JsonPropertyName("condition")] RespuestaCondicion? Condition);

    private sealed record RespuestaCondicion(
        [property: JsonPropertyName("text")] string? Text);

    private sealed record RespuestaError(
        [property: JsonPropertyName("code")] int Codigo);
}
