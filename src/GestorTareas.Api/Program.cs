using GestorTareas.ControlAcceso;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var cadenaConexion = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(cadenaConexion))
{
    throw new InvalidOperationException(
        "La cadena de conexión 'Default' es obligatoria. Configúrala mediante la variable de entorno ConnectionStrings__Default.");
}

builder.Services.AddDbContext<ContextoControlAcceso>(opciones =>
    opciones.UseSqlServer(cadenaConexion));
builder.Services.AddScoped<ServicioRegistro>();

var app = builder.Build();

app.UseExceptionHandler(manejador =>
{
    manejador.Run(async contexto =>
    {
        var excepcion = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (excepcion is BadHttpRequestException)
        {
            contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
            await contexto.Response.WriteAsJsonAsync(new
            {
                errores = new[] { "Los datos enviados no tienen un formato válido." }
            });
            return;
        }

        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await contexto.Response.WriteAsJsonAsync(new
        {
            errores = new[] { "Ocurrió un error inesperado. Inténtalo de nuevo." }
        });
    });
});

app.MapGet("/salud", () => "OK");

app.MapPost("/api/registro", async (
    SolicitudRegistro? solicitud,
    ServicioRegistro servicioRegistro) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new
        {
            errores = new[] { "Los datos de registro son obligatorios." }
        });
    }

    var resultado = await servicioRegistro.RegistrarAsync(
        solicitud.Nombre,
        solicitud.Correo,
        solicitud.Contrasena);

    return resultado.Estado switch
    {
        EstadoRegistro.Creado => Results.Json(
            new
            {
                mensaje = "Cuenta creada correctamente.",
                id = resultado.IdUsuario
            },
            statusCode: StatusCodes.Status201Created),
        EstadoRegistro.DatosInvalidos => Results.BadRequest(new { errores = resultado.Errores }),
        EstadoRegistro.CorreoDuplicado => Results.Conflict(new { errores = resultado.Errores }),
        EstadoRegistro.ErrorInterno => Results.Json(
            new { errores = resultado.Errores },
            statusCode: StatusCodes.Status500InternalServerError),
        _ => Results.Json(
            new { errores = new[] { "Ocurrió un error inesperado. Inténtalo de nuevo." } },
            statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.Run();

public sealed record SolicitudRegistro(
    string? Nombre,
    string? Correo,
    string? Contrasena);
