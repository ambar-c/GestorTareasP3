using System.Security.Claims;
using GestorTareas.Api;
using GestorTareas.ControlAcceso;
using GestorTareas.Notificaciones;
using Microsoft.AspNetCore.Authentication;
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
builder.Services.AddScoped<ServicioRecuperacion>();
builder.Services.AddScoped<ServicioContrasenas>();
builder.Services.AddScoped<ServicioSesion>();
builder.Services.AddScoped<ServicioAutenticacion>();
builder.Services.AddScoped<InicializadorAdministrador>();
builder.Services.AddScoped<ServicioUsuarios>();
builder.Services.AddAuthentication("Token")
    .AddScheme<AuthenticationSchemeOptions, ManejadorAutenticacionToken>("Token", _ => { });
builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy(Politicas.Autenticado, politica => politica.RequireAuthenticatedUser());
    opciones.AddPolicy(Politicas.Administrador, politica => politica
        .RequireAuthenticatedUser()
        .RequireRole("Administrador"));
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/salud", () => "OK")
    .AllowAnonymous();

app.MapPost("/api/login", async (
    SolicitudInicioSesion? solicitud,
    ServicioAutenticacion servicioAutenticacion) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { errores = new[] { "Los datos de inicio de sesión son obligatorios." } });
    }

    ResultadoInicioSesion resultado = await servicioAutenticacion.IniciarSesionAsync(
        solicitud.Correo,
        solicitud.Contrasena);

    return resultado.Estado switch
    {
        EstadoInicioSesion.Exitoso => Results.Ok(new
        {
            token = resultado.Token,
            vencimiento = resultado.Vencimiento
        }),
        EstadoInicioSesion.DatosInvalidos => Results.BadRequest(new { errores = resultado.Errores }),
        EstadoInicioSesion.CredencialesInvalidas => Results.Json(
            new { errores = resultado.Errores }, statusCode: StatusCodes.Status401Unauthorized),
        EstadoInicioSesion.CuentaBloqueada => Results.Json(
            new { errores = resultado.Errores }, statusCode: StatusCodes.Status423Locked),
        EstadoInicioSesion.CuentaInactiva => Results.Json(
            new { errores = resultado.Errores }, statusCode: StatusCodes.Status403Forbidden),
        EstadoInicioSesion.CuentaDesactivada => Results.Json(
            new { errores = resultado.Errores }, statusCode: StatusCodes.Status403Forbidden),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
})
    .AllowAnonymous();

app.MapGet("/api/yo", (ClaimsPrincipal usuario) =>
{
    return Results.Ok(new
    {
        id = Guid.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!),
        nombre = usuario.FindFirstValue(ClaimTypes.Name)!,
        correo = usuario.FindFirstValue(ClaimTypes.Email)!,
        rol = usuario.FindFirstValue(ClaimTypes.Role)!
    });
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapPost("/api/logout", async (
    HttpRequest solicitud,
    ServicioSesion servicioSesion) =>
{
    bool revocada = await servicioSesion.RevocarAsync(TokenHttp.Obtener(solicitud));
    return revocada ? Results.NoContent() : Results.Unauthorized();
})
    .RequireAuthorization(Politicas.Autenticado);

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
})
    .AllowAnonymous();

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
})
    .AllowAnonymous();

app.MapPost("/api/contrasena/recuperar", async (
    SolicitudRecuperacion? solicitud,
    ServicioRecuperacion servicioRecuperacion) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    ResultadoValidacionRegistro validacion = ValidadorRegistro.ValidarCorreo(solicitud.Correo);
    if (!validacion.EsValido)
    {
        return Results.BadRequest(new { error = validacion.Errores[0] });
    }

    await servicioRecuperacion.SolicitarAsync(solicitud.Correo);

    return Results.Ok(new
    {
        mensaje = "Si el correo está registrado, recibirás un código para restablecer tu contraseña."
    });
})
    .AllowAnonymous();

app.MapPost("/api/contrasena/restablecer", async (
    SolicitudRestablecimiento? solicitud,
    ServicioRecuperacion servicioRecuperacion) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    if (string.IsNullOrWhiteSpace(solicitud.Codigo))
    {
        return Results.BadRequest(new { error = "El código es obligatorio." });
    }

    if (string.IsNullOrEmpty(solicitud.ContrasenaNueva))
    {
        return Results.BadRequest(new { error = "La contraseña es obligatoria." });
    }

    ResultadoRestablecimiento resultado = await servicioRecuperacion.RestablecerAsync(
        solicitud.Codigo,
        solicitud.ContrasenaNueva);

    return resultado.Estado switch
    {
        EstadoRestablecimiento.Exitoso => Results.Ok(new
        {
            mensaje = "Tu contraseña se actualizó. Inicia sesión con la nueva contraseña."
        }),
        EstadoRestablecimiento.DatosInvalidos => Results.BadRequest(new { error = resultado.Errores[0] }),
        EstadoRestablecimiento.CodigoInvalido => Results.BadRequest(new { error = resultado.Errores[0] }),
        _ => Results.BadRequest(new { error = "No se pudo restablecer la contraseña." })
    };
})
    .AllowAnonymous();

app.MapPut("/api/contrasena", async (
    SolicitudCambioContrasena? solicitud,
    ClaimsPrincipal usuarioAutenticado,
    ServicioContrasenas servicioContrasenas) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    if (string.IsNullOrEmpty(solicitud.ContrasenaActual))
    {
        return Results.BadRequest(new { error = "La contraseña actual es obligatoria." });
    }

    if (string.IsNullOrEmpty(solicitud.ContrasenaNueva))
    {
        return Results.BadRequest(new { error = "La contraseña nueva es obligatoria." });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoEstablecerContrasena resultado = await servicioContrasenas.CambiarAsync(
        idUsuario,
        solicitud.ContrasenaActual,
        solicitud.ContrasenaNueva);

    return resultado.Exitoso
        ? Results.Ok(new
        {
            mensaje = "Tu contraseña se actualizó. Inicia sesión de nuevo."
        })
        : Results.BadRequest(new { error = resultado.Errores[0] });
})
    .RequireAuthorization(Politicas.Autenticado);

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
})
    .AllowAnonymous();

app.MapPut("/api/usuarios/{id}/rol", async (
    string id,
    SolicitudCambioRol? solicitud,
    ClaimsPrincipal administrador,
    ServicioUsuarios servicioUsuarios) =>
{
    if (!Guid.TryParse(id, out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "El identificador del usuario no es válido." });
    }

    if (solicitud is null || string.IsNullOrWhiteSpace(solicitud.Rol))
    {
        return Results.BadRequest(new { error = "El cuerpo y el rol son obligatorios." });
    }

    Rol nuevoRol = solicitud.Rol switch
    {
        "Administrador" => Rol.Administrador,
        "Estandar" => Rol.Estandar,
        _ => (Rol)(-1)
    };

    if (!Enum.IsDefined(nuevoRol))
    {
        return Results.BadRequest(new { error = "El rol no es válido." });
    }

    if (!Guid.TryParse(administrador.FindFirstValue(ClaimTypes.NameIdentifier), out Guid idAdministrador))
    {
        return Results.BadRequest(new { error = "La identidad del Administrador no es válida." });
    }

    ResultadoCambioRol resultado = await servicioUsuarios.CambiarRolAsync(
        idAdministrador,
        idUsuario,
        nuevoRol);

    return resultado.Estado switch
    {
        EstadoCambioRol.UsuarioNoEncontrado => Results.NotFound(new { error = resultado.Motivo }),
        EstadoCambioRol.MismoUsuario => Results.BadRequest(new { error = resultado.Motivo }),
        EstadoCambioRol.Exitoso => Results.Ok(new
        {
            id = resultado.Usuario!.Id,
            nombre = resultado.Usuario.Nombre,
            correo = resultado.Usuario.Correo,
            rol = resultado.Usuario.Rol.ToString()
        }),
        _ => Results.BadRequest(new { error = "No se pudo cambiar el rol." })
    };
})
    .RequireAuthorization(Politicas.Administrador);

app.MapGet("/api/usuarios", async (ServicioUsuarios servicioUsuarios) =>
{
    List<UsuarioResumen> usuarios = await servicioUsuarios.ListarAsync();
    return Results.Ok(usuarios.Select(usuario => new
    {
        id = usuario.Id,
        nombre = usuario.Nombre,
        correo = usuario.Correo,
        rol = usuario.Rol.ToString(),
        activo = usuario.Activo,
        desactivado = usuario.Desactivado
    }));
})
    .RequireAuthorization(Politicas.Administrador);

app.MapPost("/api/usuarios/{id}/desactivar", async (
    string id,
    ClaimsPrincipal administrador,
    ServicioUsuarios servicioUsuarios) =>
{
    if (!Guid.TryParse(id, out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "El identificador del usuario no es válido." });
    }

    if (!Guid.TryParse(administrador.FindFirstValue(ClaimTypes.NameIdentifier), out Guid idAdministrador))
    {
        return Results.BadRequest(new { error = "La identidad del Administrador no es válida." });
    }

    ResultadoCambioEstadoUsuario resultado = await servicioUsuarios.DesactivarAsync(
        idAdministrador,
        idUsuario);

    return resultado.Estado switch
    {
        EstadoCambioEstadoUsuario.UsuarioNoEncontrado => Results.NotFound(new { error = resultado.Motivo }),
        EstadoCambioEstadoUsuario.MismoUsuario => Results.BadRequest(new { error = resultado.Motivo }),
        EstadoCambioEstadoUsuario.Exitoso => Results.Ok(new
        {
            id = resultado.Usuario!.Id,
            nombre = resultado.Usuario.Nombre,
            correo = resultado.Usuario.Correo,
            rol = resultado.Usuario.Rol.ToString(),
            activo = resultado.Usuario.Activo,
            desactivado = resultado.Usuario.Desactivado
        }),
        _ => Results.BadRequest(new { error = "No se pudo desactivar el usuario." })
    };
})
    .RequireAuthorization(Politicas.Administrador);

app.MapPost("/api/usuarios/{id}/restablecer-contrasena", async (
    string id,
    ClaimsPrincipal administrador,
    ServicioUsuarios servicioUsuarios) =>
{
    if (!Guid.TryParse(id, out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "El identificador del usuario no es válido." });
    }

    if (!Guid.TryParse(
        administrador.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idAdministrador))
    {
        return Results.BadRequest(new { error = "La identidad del Administrador no es válida." });
    }

    ResultadoRestablecimientoForzado resultado = await servicioUsuarios
        .ForzarRestablecimientoAsync(idAdministrador, idUsuario);

    return resultado.Estado switch
    {
        EstadoRestablecimientoForzado.UsuarioNoEncontrado => Results.NotFound(
            new { error = resultado.Motivo }),
        EstadoRestablecimientoForzado.MismoUsuario => Results.BadRequest(
            new { error = resultado.Motivo }),
        EstadoRestablecimientoForzado.Exitoso => Results.Ok(new
        {
            mensaje = "Se envió al usuario un código para definir una nueva contraseña."
        }),
        _ => Results.BadRequest(new { error = "No se pudo restablecer la contraseña." })
    };
})
    .RequireAuthorization(Politicas.Administrador);

app.MapPost("/api/usuarios/{id}/reactivar", async (
    string id,
    ClaimsPrincipal administrador,
    ServicioUsuarios servicioUsuarios) =>
{
    if (!Guid.TryParse(id, out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "El identificador del usuario no es válido." });
    }

    if (!Guid.TryParse(administrador.FindFirstValue(ClaimTypes.NameIdentifier), out Guid idAdministrador))
    {
        return Results.BadRequest(new { error = "La identidad del Administrador no es válida." });
    }

    ResultadoCambioEstadoUsuario resultado = await servicioUsuarios.ReactivarAsync(
        idAdministrador,
        idUsuario);

    return resultado.Estado switch
    {
        EstadoCambioEstadoUsuario.UsuarioNoEncontrado => Results.NotFound(new { error = resultado.Motivo }),
        EstadoCambioEstadoUsuario.Exitoso => Results.Ok(new
        {
            id = resultado.Usuario!.Id,
            nombre = resultado.Usuario.Nombre,
            correo = resultado.Usuario.Correo,
            rol = resultado.Usuario.Rol.ToString(),
            activo = resultado.Usuario.Activo,
            desactivado = resultado.Usuario.Desactivado
        }),
        _ => Results.BadRequest(new { error = "No se pudo reactivar el usuario." })
    };
})
    .RequireAuthorization(Politicas.Administrador);

string? adminNombre = Environment.GetEnvironmentVariable("ADMIN_NOMBRE");
string? adminCorreo = Environment.GetEnvironmentVariable("ADMIN_CORREO");
string? adminClave = Environment.GetEnvironmentVariable("ADMIN_CLAVE");

if (string.IsNullOrWhiteSpace(adminNombre)
    || string.IsNullOrWhiteSpace(adminCorreo)
    || string.IsNullOrWhiteSpace(adminClave))
{
    app.Logger.LogWarning("No se creó el Administrador inicial: faltan variables ADMIN_*");
}
else
{
    using IServiceScope alcance = app.Services.CreateScope();
    InicializadorAdministrador inicializador = alcance.ServiceProvider
        .GetRequiredService<InicializadorAdministrador>();
    ResultadoInicializacionAdministrador resultado = await inicializador.CrearAsync(
        adminNombre,
        adminCorreo,
        adminClave);

    if (resultado.Estado == EstadoInicializacionAdministrador.DatosInvalidos)
    {
        app.Logger.LogWarning(
            "No se creó el Administrador inicial: la configuración no cumple la política requerida.");
    }
}

app.Run();

public sealed record SolicitudRegistro(
    string? Nombre,
    string? Correo,
    string? Contrasena);

public sealed record SolicitudReenvio(string? Correo);

public sealed record SolicitudRecuperacion(string? Correo);

public sealed record SolicitudRestablecimiento(string? Codigo, string? ContrasenaNueva);

public sealed record SolicitudCambioContrasena(string? ContrasenaActual, string? ContrasenaNueva);

public sealed record SolicitudInicioSesion(string? Correo, string? Contrasena);

public sealed record SolicitudCambioRol(string? Rol);
