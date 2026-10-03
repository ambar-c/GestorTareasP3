using GestorTareas.ControlAcceso;
using GestorTareas.Notificaciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

var cadenaConexion = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(cadenaConexion))
{
    throw new InvalidOperationException(
        "La cadena de conexión 'Default' es obligatoria. Configúrala mediante la variable de entorno ConnectionStrings__Default.");
}

builder.Services.AddDbContext<ContextoControlAcceso>(opciones =>
    opciones.UseSqlServer(cadenaConexion));
builder.Services.AddDbContext<ContextoNotificaciones>(opciones =>
    opciones.UseSqlServer(
        cadenaConexion,
        opcionesSql => opcionesSql.MigrationsHistoryTable("__EFMigrationsHistory_Notificaciones")));
builder.Services.AddScoped<IColaCorreos, ColaCorreos>();

var urlBase = builder.Configuration["APP_URL_BASE"];
if (string.IsNullOrWhiteSpace(urlBase))
{
    throw new InvalidOperationException("Falta la variable de entorno APP_URL_BASE.");
}

builder.Services.AddSingleton(new OpcionesActivacion(urlBase));
builder.Services.AddScoped<ServicioRegistro>();
builder.Services.AddScoped<ServicioActivacion>();

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

app.MapGet("/api/activar", async (
    string? token,
    ServicioActivacion servicioActivacion) =>
{
    var resultado = await servicioActivacion.ActivarAsync(token);

    return resultado.Estado switch
    {
        EstadoActivacion.Activada => Results.Ok(new { mensaje = resultado.Mensaje }),
        EstadoActivacion.EnlaceInvalido => Results.BadRequest(new { errores = new[] { resultado.Mensaje } }),
        EstadoActivacion.ErrorInterno => Results.Json(
            new { errores = new[] { resultado.Mensaje } },
            statusCode: StatusCodes.Status500InternalServerError),
        _ => Results.Json(
            new { errores = new[] { "Ocurrió un error inesperado. Inténtalo de nuevo." } },
            statusCode: StatusCodes.Status500InternalServerError)
    };
});

app.MapPost("/api/activacion/reenviar", async (
    SolicitudReenvio? solicitud,
    ServicioActivacion servicioActivacion) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new
        {
            errores = new[] { "Los datos son obligatorios." }
        });
    }

    var resultado = await servicioActivacion.ReenviarAsync(solicitud.Correo);

    return resultado.Estado switch
    {
        EstadoReenvio.Aceptado => Results.Ok(new { mensaje = resultado.Mensaje }),
        EstadoReenvio.DatosInvalidos => Results.BadRequest(new { errores = resultado.Errores }),
        EstadoReenvio.ErrorInterno => Results.Json(
            new { errores = new[] { resultado.Mensaje } },
            statusCode: StatusCodes.Status500InternalServerError),
        _ => Results.Json(
            new { errores = new[] { "Ocurrió un error inesperado. Inténtalo de nuevo." } },
            statusCode: StatusCodes.Status500InternalServerError)
    };
});

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

public sealed record SolicitudReenvio(string? Correo);
