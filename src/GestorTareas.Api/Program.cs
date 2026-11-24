using System.Security.Claims;
using System.Globalization;
using GestorTareas.Api;
using GestorTareas.ControlAcceso;
using GestorTareas.Documentos;
using GestorTareas.GestionPermisos;
using GestorTareas.Notificaciones;
using GestorTareas.Negocio;
using GestorTareas.Negocio.Entidades;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

var cadenaConexion = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(cadenaConexion))
{
    throw new InvalidOperationException(
        "La cadena de conexión 'Default' es obligatoria. Configúrala mediante la variable de entorno ConnectionStrings__Default.");
}

builder.Services.AddDbContext<ContextoControlAcceso>(opciones =>
    opciones.UseSqlServer(cadenaConexion));
builder.Services.AddDbContext<ContextoDocumentos>(opciones =>
    opciones.UseSqlServer(
        cadenaConexion,
        opcionesSql => opcionesSql.MigrationsHistoryTable("__EFMigrationsHistory_Documentos")));
builder.Services.AddDbContext<ContextoGestionPermisos>(opciones =>
    opciones.UseSqlServer(
        cadenaConexion,
        opcionesSql => opcionesSql.MigrationsHistoryTable("__EFMigrationsHistory_GestionPermisos")));
builder.Services.AddDbContext<ContextoNotificaciones>(opciones =>
    opciones.UseSqlServer(
        cadenaConexion,
        opcionesSql => opcionesSql.MigrationsHistoryTable("__EFMigrationsHistory_Notificaciones")));
builder.Services.AddDbContext<NegocioDbContext>(opciones =>
    opciones.UseSqlServer(
        cadenaConexion,
        opcionesSql => opcionesSql.MigrationsHistoryTable("__EFMigrationsHistory_Negocio")));
builder.Services.AddSingleton(new OpcionesClima(builder.Configuration["CLIMA_API_KEY"]));
builder.Services.AddHttpClient<IServicioClima, ServicioClima>(cliente =>
    cliente.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddScoped<IColaCorreos, ColaCorreos>();
builder.Services.AddScoped<ServicioNotificaciones>();

string? rutaDocumentos = builder.Configuration["DOCUMENTOS_RUTA"];
if (string.IsNullOrWhiteSpace(rutaDocumentos))
{
    rutaDocumentos = "datos/documentos";
}
builder.Services.AddSingleton(new OpcionesAlmacenamientoDocumentos(rutaDocumentos));

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
builder.Services.AddScoped<ServicioSolicitudesPermiso>();
builder.Services.AddScoped<ServicioDocumentos>();
builder.Services.AddScoped<ServicioProyectos>();
builder.Services.AddScoped<ServicioTareas>();
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

app.MapPost("/api/solicitudes-permisos", async (
    SolicitudCrearPermiso? solicitud,
    ClaimsPrincipal usuarioAutenticado,
    ServicioSolicitudesPermiso servicioSolicitudesPermiso,
    ServicioUsuarios servicioUsuarios,
    ServicioNotificaciones servicioNotificaciones) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idSolicitante))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoCrearSolicitudPermiso resultado = await servicioSolicitudesPermiso.CrearAsync(
        idSolicitante,
        solicitud.Permiso);

    if (!resultado.Exito)
    {
        return Results.BadRequest(new { error = resultado.Error });
    }

    List<UsuarioResumen> administradores =
        await servicioUsuarios.ListarAdministradoresActivosAsync();
    string nombreSolicitante = usuarioAutenticado.FindFirstValue(ClaimTypes.Name)
        ?? "Un usuario";
    string mensaje = $"{nombreSolicitante} solicitó el permiso {solicitud.Permiso}.";

    foreach (UsuarioResumen administrador in administradores)
    {
        await servicioNotificaciones.NotificarAsync(
            administrador.Id,
            administrador.Correo,
            "Nueva solicitud de permiso",
            mensaje);
    }

    return Results.Created(
        $"/api/solicitudes-permisos/{resultado.Solicitud!.Id}",
        resultado.Solicitud);
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapGet("/api/solicitudes-permisos/pendientes", async (
    ServicioSolicitudesPermiso servicioSolicitudesPermiso) =>
{
    List<GestorTareas.GestionPermisos.Entidades.SolicitudPermiso> solicitudes =
        await servicioSolicitudesPermiso.ListarPendientesAsync();
    return Results.Ok(solicitudes);
})
    .RequireAuthorization(Politicas.Administrador);

app.MapPost("/api/solicitudes-permisos/{id:guid}/aprobar", async (
    Guid id,
    ClaimsPrincipal administrador,
    ServicioSolicitudesPermiso servicioSolicitudesPermiso,
    ServicioUsuarios servicioUsuarios,
    ServicioNotificaciones servicioNotificaciones) =>
{
    if (!Guid.TryParse(
        administrador.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idAdministrador))
    {
        return Results.BadRequest(new { error = "La identidad del Administrador no es válida." });
    }

    ResultadoResolucionSolicitudPermiso resultado =
        await servicioSolicitudesPermiso.AprobarAsync(id, idAdministrador);

    if (resultado.Estado == EstadoResolucionSolicitudPermiso.Exitoso)
    {
        string? correo = await servicioUsuarios.ObtenerCorreoAsync(
            resultado.Solicitud!.IdSolicitante);
        if (correo is not null)
        {
            await servicioNotificaciones.NotificarAsync(
                resultado.Solicitud.IdSolicitante,
                correo,
                "Solicitud aprobada",
                $"Tu solicitud del permiso {resultado.Solicitud.PermisoSolicitado} fue aprobada.");
        }
    }

    return resultado.Estado switch
    {
        EstadoResolucionSolicitudPermiso.SolicitudNoEncontrada =>
            Results.NotFound(new { error = resultado.Motivo }),
        EstadoResolucionSolicitudPermiso.MismoSolicitante =>
            Results.Json(new { error = resultado.Motivo }, statusCode: StatusCodes.Status403Forbidden),
        EstadoResolucionSolicitudPermiso.TransicionNoValida =>
            Results.Conflict(new { error = resultado.Motivo }),
        EstadoResolucionSolicitudPermiso.Exitoso =>
            Results.Ok(resultado.Solicitud),
        _ => Results.BadRequest(new { error = resultado.Motivo })
    };
})
    .RequireAuthorization(Politicas.Administrador);

app.MapPost("/api/solicitudes-permisos/{id:guid}/rechazar", async (
    Guid id,
    SolicitudRechazarPermiso? solicitud,
    ClaimsPrincipal administrador,
    ServicioSolicitudesPermiso servicioSolicitudesPermiso,
    ServicioUsuarios servicioUsuarios,
    ServicioNotificaciones servicioNotificaciones) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    if (!Guid.TryParse(
        administrador.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idAdministrador))
    {
        return Results.BadRequest(new { error = "La identidad del Administrador no es válida." });
    }

    ResultadoResolucionSolicitudPermiso resultado =
        await servicioSolicitudesPermiso.RechazarAsync(id, idAdministrador, solicitud.Motivo);

    if (resultado.Estado == EstadoResolucionSolicitudPermiso.Exitoso)
    {
        string? correo = await servicioUsuarios.ObtenerCorreoAsync(
            resultado.Solicitud!.IdSolicitante);
        if (correo is not null)
        {
            await servicioNotificaciones.NotificarAsync(
                resultado.Solicitud.IdSolicitante,
                correo,
                "Solicitud rechazada",
                $"Tu solicitud del permiso {resultado.Solicitud.PermisoSolicitado} fue rechazada. Motivo: {resultado.Solicitud.MotivoRechazo}");
        }
    }

    return resultado.Estado switch
    {
        EstadoResolucionSolicitudPermiso.SolicitudNoEncontrada =>
            Results.NotFound(new { error = resultado.Motivo }),
        EstadoResolucionSolicitudPermiso.MismoSolicitante =>
            Results.Json(new { error = resultado.Motivo }, statusCode: StatusCodes.Status403Forbidden),
        EstadoResolucionSolicitudPermiso.TransicionNoValida =>
            Results.Conflict(new { error = resultado.Motivo }),
        EstadoResolucionSolicitudPermiso.MotivoRechazoInvalido =>
            Results.BadRequest(new { error = resultado.Motivo }),
        EstadoResolucionSolicitudPermiso.Exitoso =>
            Results.Ok(resultado.Solicitud),
        _ => Results.BadRequest(new { error = resultado.Motivo })
    };
})
    .RequireAuthorization(Politicas.Administrador);

app.MapGet("/api/solicitudes-permisos", async (
    ClaimsPrincipal usuarioAutenticado,
    ServicioSolicitudesPermiso servicioSolicitudesPermiso) =>
{
    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idSolicitante))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    List<GestorTareas.GestionPermisos.Entidades.SolicitudPermiso> solicitudes =
        await servicioSolicitudesPermiso.ListarPropiasAsync(idSolicitante);
    return Results.Ok(solicitudes);
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapPost("/api/documentos", async (
    IFormFile? archivo,
    ClaimsPrincipal usuarioAutenticado,
    ServicioDocumentos servicioDocumentos) =>
{
    if (archivo is null)
    {
        return Results.BadRequest(new { error = "El archivo es obligatorio." });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    await using Stream contenido = archivo.OpenReadStream();
    ResultadoSubirDocumento resultado = await servicioDocumentos.SubirAsync(
        idUsuario,
        archivo.FileName,
        archivo.ContentType,
        contenido);

    if (!resultado.Exito)
    {
        return Results.BadRequest(new { error = resultado.Error });
    }

    return Results.Created(
        $"/api/documentos/{resultado.Documento!.Id}/descargar",
        new
        {
            id = resultado.Documento.Id,
            nombreOriginal = resultado.Documento.NombreOriginal,
            tipoContenido = resultado.Documento.TipoContenido,
            tamanoBytes = resultado.Documento.TamanoBytes,
            fechaSubida = resultado.Documento.FechaSubida,
            idUsuarioPropietario = resultado.Documento.IdUsuarioPropietario
        });
})
    .RequireAuthorization(Politicas.Autenticado)
    .DisableAntiforgery();

app.MapGet("/api/documentos", async (
    ClaimsPrincipal usuarioAutenticado,
    ServicioDocumentos servicioDocumentos) =>
{
    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoListarDocumentos resultado = await servicioDocumentos.ListarAsync(
        idUsuario,
        usuarioAutenticado.IsInRole("Administrador"));

    return Results.Ok(resultado.Documentos.Select(documento => new
    {
        id = documento.Id,
        nombreOriginal = documento.NombreOriginal,
        tipoContenido = documento.TipoContenido,
        tamanoBytes = documento.TamanoBytes,
        fechaSubida = documento.FechaSubida,
        idUsuarioPropietario = documento.IdUsuarioPropietario
    }));
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapGet("/api/documentos/{id:guid}/descargar", async (
    Guid id,
    ClaimsPrincipal usuarioAutenticado,
    ServicioDocumentos servicioDocumentos) =>
{
    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoDescargarDocumento resultado = await servicioDocumentos.DescargarAsync(
        idUsuario,
        usuarioAutenticado.IsInRole("Administrador"),
        id);

    return resultado.Estado switch
    {
        EstadoOperacionDocumento.Exitoso => Results.File(
            resultado.Contenido!,
            resultado.TipoContenido!,
            resultado.NombreOriginal!),
        EstadoOperacionDocumento.NoEncontrado => Results.NotFound(new { error = resultado.Error }),
        EstadoOperacionDocumento.SinPermiso => Results.Json(
            new { error = resultado.Error },
            statusCode: StatusCodes.Status403Forbidden),
        EstadoOperacionDocumento.ArchivoNoDisponible => Results.Conflict(
            new { error = resultado.Error }),
        _ => Results.BadRequest(new { error = resultado.Error })
    };
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapDelete("/api/documentos/{id:guid}", async (
    Guid id,
    ClaimsPrincipal usuarioAutenticado,
    ServicioDocumentos servicioDocumentos) =>
{
    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoEliminarDocumento resultado = await servicioDocumentos.EliminarAsync(
        idUsuario,
        usuarioAutenticado.IsInRole("Administrador"),
        id);

    return resultado.Estado switch
    {
        EstadoOperacionDocumento.Exitoso => Results.NoContent(),
        EstadoOperacionDocumento.NoEncontrado => Results.NotFound(new { error = resultado.Error }),
        EstadoOperacionDocumento.SinPermiso => Results.Json(
            new { error = resultado.Error },
            statusCode: StatusCodes.Status403Forbidden),
        _ => Results.BadRequest(new { error = resultado.Error })
    };
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapPost("/api/proyectos", async (
    SolicitudCrearProyecto? solicitud,
    ClaimsPrincipal usuarioAutenticado,
    ServicioProyectos servicioProyectos) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuarioCreador))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoCrearProyecto resultado = await servicioProyectos.CrearAsync(
        idUsuarioCreador,
        solicitud.Nombre,
        usuarioAutenticado.IsInRole("Administrador"));

    return resultado.Estado switch
    {
        EstadoCrearProyecto.Exitoso => Results.Created(
            $"/api/proyectos/{resultado.Proyecto!.Id}",
            resultado.Proyecto),
        EstadoCrearProyecto.SinPermiso => Results.Json(
            new { error = resultado.Error },
            statusCode: StatusCodes.Status403Forbidden),
        EstadoCrearProyecto.NombreInvalido => Results.BadRequest(new { error = resultado.Error }),
        _ => Results.BadRequest(new { error = resultado.Error })
    };
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapGet("/api/proyectos/{id:guid}/progreso", async (
    Guid id,
    ClaimsPrincipal usuarioAutenticado,
    ServicioProyectos servicioProyectos) =>
{
    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoProgresoProyecto resultado = await servicioProyectos.ObtenerProgresoAsync(
        idUsuario,
        usuarioAutenticado.IsInRole("Administrador"),
        id);

    return resultado.Estado switch
    {
        EstadoConsultaProyecto.Exitoso => Results.Ok(resultado.Progreso),
        EstadoConsultaProyecto.NoEncontrado => Results.NotFound(new { error = resultado.Error }),
        EstadoConsultaProyecto.SinPermiso => Results.Json(
            new { error = resultado.Error },
            statusCode: StatusCodes.Status403Forbidden),
        _ => Results.BadRequest(new { error = resultado.Error })
    };
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapPut("/api/proyectos/{id:guid}/ciudad", async (
    Guid id,
    SolicitudAsignarCiudad? solicitud,
    ClaimsPrincipal usuarioAutenticado,
    ServicioProyectos servicioProyectos) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoAsignarCiudadProyecto resultado = await servicioProyectos.AsignarCiudadAsync(
        idUsuario,
        usuarioAutenticado.IsInRole("Administrador"),
        id,
        solicitud.Ciudad);

    return resultado.Estado switch
    {
        EstadoAsignarCiudadProyecto.Exitoso => Results.Ok(new
        {
            id = resultado.Proyecto!.Id,
            nombre = resultado.Proyecto.Nombre,
            ciudad = resultado.Proyecto.Ciudad
        }),
        EstadoAsignarCiudadProyecto.NoEncontrado => Results.NotFound(new { error = resultado.Error }),
        EstadoAsignarCiudadProyecto.SinPermiso => Results.Json(
            new { error = resultado.Error },
            statusCode: StatusCodes.Status403Forbidden),
        EstadoAsignarCiudadProyecto.DatosInvalidos => Results.BadRequest(new { error = resultado.Error }),
        _ => Results.BadRequest(new { error = resultado.Error })
    };
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapGet("/api/proyectos/{id:guid}/resumen", async (
    Guid id,
    ClaimsPrincipal usuarioAutenticado,
    ServicioProyectos servicioProyectos,
    NegocioDbContext contexto,
    IServicioClima servicioClima) =>
{
    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoProgresoProyecto resultadoProgreso = await servicioProyectos.ObtenerProgresoAsync(
        idUsuario,
        usuarioAutenticado.IsInRole("Administrador"),
        id);

    if (resultadoProgreso.Estado != EstadoConsultaProyecto.Exitoso)
    {
        return resultadoProgreso.Estado switch
        {
            EstadoConsultaProyecto.NoEncontrado => Results.NotFound(new { error = resultadoProgreso.Error }),
            EstadoConsultaProyecto.SinPermiso => Results.Json(
                new { error = resultadoProgreso.Error },
                statusCode: StatusCodes.Status403Forbidden),
            _ => Results.BadRequest(new { error = resultadoProgreso.Error })
        };
    }

    Proyecto? proyecto = await contexto.Proyectos
        .AsNoTracking()
        .SingleOrDefaultAsync(proyecto => proyecto.Id == id);

    if (proyecto?.Ciudad is null)
    {
        return Results.Ok(new
        {
            progreso = resultadoProgreso.Progreso,
            clima = (object?)null
        });
    }

    ResultadoClima resultadoClima = await servicioClima.ObtenerClimaActualAsync(proyecto.Ciudad);
    return resultadoClima.Estado switch
    {
        EstadoClima.Exitoso => Results.Ok(new
        {
            progreso = resultadoProgreso.Progreso,
            clima = new
            {
                temperatura = resultadoClima.Clima!.TemperaturaCelsius,
                humedad = resultadoClima.Clima.Humedad,
                descripcion = resultadoClima.Clima.DescripcionCondicion,
                ciudad = resultadoClima.Clima.Ciudad
            }
        }),
        EstadoClima.SinClave => Results.Json(
            new { error = "El servicio de clima no está configurado en este momento." },
            statusCode: StatusCodes.Status503ServiceUnavailable),
        EstadoClima.CiudadNoEncontrada => Results.Json(
            new { error = "No se encontró la ciudad configurada para el proyecto." },
            statusCode: StatusCodes.Status503ServiceUnavailable),
        EstadoClima.ApiNoDisponible => Results.Json(
            new { error = "El servicio de clima no está disponible en este momento." },
            statusCode: StatusCodes.Status503ServiceUnavailable),
        _ => Results.Json(
            new { error = "El servicio de clima no está disponible en este momento." },
            statusCode: StatusCodes.Status503ServiceUnavailable)
    };
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapPost("/api/proyectos/{id:guid}/tareas", async (
    Guid id,
    SolicitudCrearTarea? solicitud,
    ClaimsPrincipal usuarioAutenticado,
    ServicioTareas servicioTareas) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    if (solicitud.IdPrioridad is null)
    {
        return Results.BadRequest(new { error = "La prioridad es obligatoria." });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoOperacionTarea resultado = await servicioTareas.CrearAsync(
        idUsuario,
        usuarioAutenticado.IsInRole("Administrador"),
        id,
        solicitud.Titulo,
        solicitud.IdPrioridad.Value);

    return resultado.Estado switch
    {
        EstadoOperacionTarea.Exitoso => Results.Created(
            $"/api/tareas/{resultado.Tarea!.Id}",
            RespuestaTarea.Desde(resultado.Tarea)),
        EstadoOperacionTarea.NoEncontrado => Results.NotFound(new { error = resultado.Error }),
        EstadoOperacionTarea.SinPermiso => Results.Json(
            new { error = resultado.Error },
            statusCode: StatusCodes.Status403Forbidden),
        EstadoOperacionTarea.DatosInvalidos => Results.BadRequest(new { error = resultado.Error }),
        _ => Results.BadRequest(new { error = resultado.Error })
    };
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapPut("/api/tareas/{id:guid}/responsable", async (
    Guid id,
    SolicitudAsignarResponsable? solicitud,
    ClaimsPrincipal usuarioAutenticado,
    ServicioTareas servicioTareas,
    ServicioUsuarios servicioUsuarios,
    ServicioNotificaciones servicioNotificaciones) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    if (solicitud.IdResponsable is null)
    {
        return Results.BadRequest(new { error = "El responsable es obligatorio." });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoOperacionTarea resultado = await servicioTareas.AsignarResponsableAsync(
        idUsuario,
        usuarioAutenticado.IsInRole("Administrador"),
        id,
        solicitud.IdResponsable.Value);

    if (resultado.Estado == EstadoOperacionTarea.Exitoso)
    {
        string? correo = await servicioUsuarios.ObtenerCorreoAsync(
            resultado.Tarea!.UsuarioAsignadoId!.Value);
        if (correo is not null)
        {
            await servicioNotificaciones.NotificarAsync(
                resultado.Tarea.UsuarioAsignadoId.Value,
                correo,
                "Tarea asignada",
                $"Se te asignó la tarea \"{resultado.Tarea.Titulo}\".");
        }
    }

    return resultado.Estado switch
    {
        EstadoOperacionTarea.Exitoso => Results.Ok(RespuestaTarea.Desde(resultado.Tarea!)),
        EstadoOperacionTarea.NoEncontrado => Results.NotFound(new { error = resultado.Error }),
        EstadoOperacionTarea.SinPermiso => Results.Json(
            new { error = resultado.Error },
            statusCode: StatusCodes.Status403Forbidden),
        EstadoOperacionTarea.DatosInvalidos => Results.BadRequest(new { error = resultado.Error }),
        _ => Results.BadRequest(new { error = resultado.Error })
    };
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapPost("/api/tareas/{id:guid}/estado", async (
    Guid id,
    SolicitudCambiarEstado? solicitud,
    ClaimsPrincipal usuarioAutenticado,
    ServicioTareas servicioTareas) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    string? nombreEstado = Enum.GetNames<EstadoTarea>()
        .FirstOrDefault(nombre => string.Equals(
            nombre,
            solicitud.Estado,
            StringComparison.OrdinalIgnoreCase));
    if (nombreEstado is null)
    {
        return Results.BadRequest(new { error = "El estado no es válido." });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    EstadoTarea estado = Enum.Parse<EstadoTarea>(nombreEstado);
    ResultadoOperacionTarea resultado = await servicioTareas.CambiarEstadoAsync(
        idUsuario,
        usuarioAutenticado.IsInRole("Administrador"),
        id,
        estado,
        solicitud.Motivo);

    return resultado.Estado switch
    {
        EstadoOperacionTarea.Exitoso => Results.Ok(RespuestaTarea.Desde(resultado.Tarea!)),
        EstadoOperacionTarea.NoEncontrado => Results.NotFound(new { error = resultado.Error }),
        EstadoOperacionTarea.SinPermiso => Results.Json(
            new { error = resultado.Error },
            statusCode: StatusCodes.Status403Forbidden),
        EstadoOperacionTarea.TransicionRechazada => Results.Conflict(new { error = resultado.Error }),
        _ => Results.BadRequest(new { error = resultado.Error })
    };
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapPost("/api/tareas/{id:guid}/comentarios", async (
    Guid id,
    SolicitudComentar? solicitud,
    ClaimsPrincipal usuarioAutenticado,
    ServicioTareas servicioTareas) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoComentarTarea resultado = await servicioTareas.ComentarAsync(
        idUsuario,
        usuarioAutenticado.IsInRole("Administrador"),
        id,
        solicitud.Texto);

    return resultado.Estado switch
    {
        EstadoOperacionTarea.Exitoso => Results.Created(
            $"/api/tareas/{id}/comentarios/{resultado.Comentario!.Id}",
            RespuestaComentario.Desde(resultado.Comentario)),
        EstadoOperacionTarea.NoEncontrado => Results.NotFound(new { error = resultado.Error }),
        EstadoOperacionTarea.SinPermiso => Results.Json(
            new { error = resultado.Error },
            statusCode: StatusCodes.Status403Forbidden),
        EstadoOperacionTarea.DatosInvalidos => Results.BadRequest(new { error = resultado.Error }),
        _ => Results.BadRequest(new { error = resultado.Error })
    };
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

app.MapGet("/api/notificaciones", async (
    ClaimsPrincipal usuarioAutenticado,
    bool? leida,
    ServicioNotificaciones servicioNotificaciones) =>
{
    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    List<Notificacion> notificaciones = await servicioNotificaciones.ListarAsync(
        idUsuario,
        leida);
    return Results.Ok(notificaciones.Select(notificacion => new
    {
        id = notificacion.Id,
        titulo = notificacion.Titulo,
        mensaje = notificacion.Mensaje,
        leida = notificacion.Leida,
        fechaCreacion = notificacion.FechaCreacion
    }));
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapGet("/api/reportes/solicitudes-permiso", async (
    ClaimsPrincipal usuarioAutenticado,
    string? desde,
    string? hasta,
    ContextoGestionPermisos contexto) =>
{
    if (!RangoFechasReportes.IntentarObtener(
        desde,
        hasta,
        out DateTime? fechaDesde,
        out DateTime? fechaHasta,
        out string? error))
    {
        return Results.BadRequest(new { error });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    IQueryable<GestorTareas.GestionPermisos.Entidades.SolicitudPermiso> solicitudes =
        contexto.SolicitudesPermiso.AsNoTracking();

    if (!usuarioAutenticado.IsInRole("Administrador"))
    {
        solicitudes = solicitudes.Where(solicitud => solicitud.IdSolicitante == idUsuario);
    }

    if (fechaDesde.HasValue)
    {
        solicitudes = solicitudes.Where(solicitud => solicitud.FechaCreacion >= fechaDesde.Value);
    }

    if (fechaHasta.HasValue)
    {
        solicitudes = solicitudes.Where(solicitud => solicitud.FechaCreacion < fechaHasta.Value);
    }

    var grupos = await solicitudes
        .GroupBy(solicitud => new
        {
            Anio = solicitud.FechaCreacion.Year,
            Mes = solicitud.FechaCreacion.Month,
            solicitud.Estado
        })
        .Select(grupo => new
        {
            grupo.Key.Anio,
            grupo.Key.Mes,
            grupo.Key.Estado,
            Total = grupo.Count()
        })
        .OrderBy(grupo => grupo.Anio)
        .ThenBy(grupo => grupo.Mes)
        .ThenBy(grupo => grupo.Estado)
        .ToListAsync();

    return Results.Ok(grupos.Select(grupo => new
    {
        periodo = $"{grupo.Anio:D4}-{grupo.Mes:D2}",
        estado = grupo.Estado.ToString(),
        total = grupo.Total
    }));
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapGet("/api/reportes/progreso-tareas", async (
    ClaimsPrincipal usuarioAutenticado,
    string? desde,
    string? hasta,
    NegocioDbContext contexto) =>
{
    if (!RangoFechasReportes.IntentarObtener(
        desde,
        hasta,
        out DateTime? fechaDesde,
        out DateTime? fechaHasta,
        out string? error))
    {
        return Results.BadRequest(new { error });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    IQueryable<Proyecto> proyectos = contexto.Proyectos.AsNoTracking();

    if (!usuarioAutenticado.IsInRole("Administrador"))
    {
        proyectos = proyectos.Where(proyecto => proyecto.UsuarioCreadorId == idUsuario);
    }

    IQueryable<Tarea> tareas = contexto.Tareas.AsNoTracking();

    if (fechaDesde.HasValue)
    {
        tareas = tareas.Where(tarea => tarea.FechaCreacion >= fechaDesde.Value);
    }

    if (fechaHasta.HasValue)
    {
        tareas = tareas.Where(tarea => tarea.FechaCreacion < fechaHasta.Value);
    }

    if (fechaDesde.HasValue || fechaHasta.HasValue)
    {
        proyectos = proyectos.Where(proyecto => tareas.Any(tarea => tarea.ProyectoId == proyecto.Id));
    }

    var progreso = await proyectos
        .Select(proyecto => new
        {
            idProyecto = proyecto.Id,
            nombre = proyecto.Nombre,
            total = tareas.Count(tarea => tarea.ProyectoId == proyecto.Id),
            pendientes = tareas.Count(tarea =>
                tarea.ProyectoId == proyecto.Id && tarea.Estado == EstadoTarea.Pendiente),
            enProgreso = tareas.Count(tarea =>
                tarea.ProyectoId == proyecto.Id && tarea.Estado == EstadoTarea.EnProgreso),
            completadas = tareas.Count(tarea =>
                tarea.ProyectoId == proyecto.Id && tarea.Estado == EstadoTarea.Completada),
            canceladas = tareas.Count(tarea =>
                tarea.ProyectoId == proyecto.Id && tarea.Estado == EstadoTarea.Cancelada)
        })
        .OrderBy(proyecto => proyecto.nombre)
        .ToListAsync();

    return Results.Ok(progreso.Select(proyecto =>
    {
        decimal porcentajeCompletado = ServicioProyectos.CalcularPorcentajeCompletado(
            proyecto.completadas,
            proyecto.total,
            proyecto.canceladas);

        return new
        {
            idProyecto = proyecto.idProyecto,
            nombre = proyecto.nombre,
            total = proyecto.total,
            pendientes = proyecto.pendientes,
            enProgreso = proyecto.enProgreso,
            completadas = proyecto.completadas,
            canceladas = proyecto.canceladas,
            porcentajeCompletado
        };
    }));
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapGet("/api/correos", async (ContextoNotificaciones contexto) =>
{
    var correos = await contexto.CorreosEnCola
        .OrderByDescending(correo => correo.FechaCreacion)
        .Select(correo => new
        {
            correo.Id,
            correo.Destinatario,
            correo.Asunto,
            correo.Estado,
            correo.Intentos,
            correo.FechaCreacion,
            correo.FechaEnvio,
            correo.UltimoError
        })
        .ToListAsync();

    return Results.Ok(correos.Select(correo => new
    {
        id = correo.Id,
        destinatario = correo.Destinatario,
        asunto = correo.Asunto,
        estado = correo.Estado.ToString(),
        intentos = correo.Intentos,
        fechaCreacion = correo.FechaCreacion,
        fechaEnvio = correo.FechaEnvio,
        ultimoError = correo.UltimoError
    }));
})
    .RequireAuthorization(Politicas.Administrador);

app.MapPost("/api/notificaciones/{id:guid}/leer", async (
    Guid id,
    ClaimsPrincipal usuarioAutenticado,
    ServicioNotificaciones servicioNotificaciones) =>
{
    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    ResultadoMarcarNotificacion resultado = await servicioNotificaciones
        .MarcarComoLeidaAsync(id, idUsuario);

    return resultado.Estado switch
    {
        EstadoMarcarNotificacion.Exitoso => Results.Ok(new
        {
            id = resultado.Notificacion!.Id,
            leida = resultado.Notificacion.Leida
        }),
        EstadoMarcarNotificacion.NotificacionNoEncontrada => Results.NotFound(
            new { error = "La notificación no existe." }),
        EstadoMarcarNotificacion.NoPerteneceAlUsuario => Results.Json(
            new { error = "La notificación no pertenece al usuario autenticado." },
            statusCode: StatusCodes.Status403Forbidden),
        _ => Results.BadRequest(new { error = "No se pudo marcar la notificación." })
    };
})
    .RequireAuthorization(Politicas.Autenticado);

app.MapPut("/api/notificaciones/preferencias", async (
    SolicitudPreferenciaNotificacion? solicitud,
    ClaimsPrincipal usuarioAutenticado,
    ServicioNotificaciones servicioNotificaciones) =>
{
    if (solicitud is null)
    {
        return Results.BadRequest(new { error = "El cuerpo de la solicitud es obligatorio." });
    }

    if (solicitud.CorreoActivo is null)
    {
        return Results.BadRequest(new { error = "correoActivo es obligatorio." });
    }

    if (!Guid.TryParse(
        usuarioAutenticado.FindFirstValue(ClaimTypes.NameIdentifier),
        out Guid idUsuario))
    {
        return Results.BadRequest(new { error = "La identidad del usuario no es válida." });
    }

    PreferenciaNotificacion preferencia = await servicioNotificaciones.GuardarPreferenciaAsync(
        idUsuario,
        solicitud.CorreoActivo.Value);
    return Results.Ok(new { correoActivo = preferencia.CorreoActivo });
})
    .RequireAuthorization(Politicas.Autenticado);

const int intentosMaximosMigraciones = 15;
for (int intento = 1; intento <= intentosMaximosMigraciones; intento++)
{
    try
    {
        using IServiceScope alcanceMigraciones = app.Services.CreateScope();
        await alcanceMigraciones.ServiceProvider
            .GetRequiredService<ContextoControlAcceso>()
            .Database.MigrateAsync();
        await alcanceMigraciones.ServiceProvider
            .GetRequiredService<ContextoGestionPermisos>()
            .Database.MigrateAsync();
        await alcanceMigraciones.ServiceProvider
            .GetRequiredService<ContextoNotificaciones>()
            .Database.MigrateAsync();
        await alcanceMigraciones.ServiceProvider
            .GetRequiredService<NegocioDbContext>()
            .Database.MigrateAsync();
        await alcanceMigraciones.ServiceProvider
            .GetRequiredService<ContextoDocumentos>()
            .Database.MigrateAsync();

        break;
    }
    catch (SqlException) when (intento < intentosMaximosMigraciones)
    {
        app.Logger.LogWarning(
            "Esperando a la base de datos (intento {Intento} de {IntentosMaximos})",
            intento,
            intentosMaximosMigraciones);
        await Task.Delay(TimeSpan.FromSeconds(4));
    }
}

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

public sealed record SolicitudCrearPermiso(string? Permiso);

public sealed record SolicitudRechazarPermiso(string? Motivo);

public sealed record SolicitudCrearProyecto(string? Nombre);

public sealed record SolicitudAsignarCiudad(string? Ciudad);

public sealed record SolicitudCrearTarea(string? Titulo, Guid? IdPrioridad);

public sealed record SolicitudAsignarResponsable(Guid? IdResponsable);

public sealed record SolicitudPreferenciaNotificacion(bool? CorreoActivo);

public sealed record SolicitudCambiarEstado(string? Estado, string? Motivo);

public sealed record SolicitudComentar(string? Texto);

public sealed record RespuestaTarea(
    Guid Id,
    string Titulo,
    Guid ProyectoId,
    Guid PrioridadId,
    Guid? UsuarioAsignadoId,
    string Estado,
    string? MotivoCancelacion,
    DateTime FechaCreacion)
{
    public static RespuestaTarea Desde(Tarea tarea) => new(
        tarea.Id,
        tarea.Titulo,
        tarea.ProyectoId,
        tarea.PrioridadId,
        tarea.UsuarioAsignadoId,
        tarea.Estado.ToString(),
        tarea.MotivoCancelacion,
        tarea.FechaCreacion);
}

public sealed record RespuestaComentario(
    Guid Id,
    Guid TareaId,
    Guid UsuarioId,
    string Texto,
    DateTime FechaCreacion)
{
    public static RespuestaComentario Desde(Comentario comentario) => new(
        comentario.Id,
        comentario.TareaId,
        comentario.UsuarioId,
        comentario.Texto,
        comentario.FechaCreacion);
}

public static class RangoFechasReportes
{
    public static bool IntentarObtener(
        string? desde,
        string? hasta,
        out DateTime? fechaDesde,
        out DateTime? fechaHasta,
        out string? error)
    {
        fechaDesde = null;
        fechaHasta = null;
        error = null;

        DateOnly? fechaInicial = null;
        if (!string.IsNullOrWhiteSpace(desde))
        {
            if (!DateOnly.TryParseExact(
                desde,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly fecha))
            {
                error = "El parámetro desde debe tener el formato YYYY-MM-DD.";
                return false;
            }

            fechaInicial = fecha;
            fechaDesde = fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        }

        if (!string.IsNullOrWhiteSpace(hasta))
        {
            if (!DateOnly.TryParseExact(
                hasta,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly fecha))
            {
                error = "El parámetro hasta debe tener el formato YYYY-MM-DD.";
                return false;
            }

            fechaHasta = fecha.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

            if (fechaInicial.HasValue && fechaInicial.Value > fecha)
            {
                error = "El parámetro desde no puede ser posterior a hasta.";
                return false;
            }
        }

        return true;
    }
}
