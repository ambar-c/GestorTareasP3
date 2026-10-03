using System.Security.Claims;
using System.Text.Encodings.Web;
using GestorTareas.ControlAcceso;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GestorTareas.Api;

public static class TokenHttp
{
    public static string? Obtener(HttpRequest solicitud)
    {
        string valor = solicitud.Headers.Authorization.ToString();
        return valor.Length > 7
            && valor.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? valor[7..].Trim()
            : null;
    }
}

public sealed class ManejadorAutenticacionToken : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ServicioSesion servicioSesion;

    public ManejadorAutenticacionToken(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ServicioSesion servicioSesion)
        : base(options, logger, encoder)
    {
        this.servicioSesion = servicioSesion;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = TokenHttp.Obtener(Request);
        if (token is null)
        {
            return AuthenticateResult.NoResult();
        }

        Usuario? usuario = await servicioSesion.ObtenerUsuarioActualAsync(token);
        if (usuario is null)
        {
            return AuthenticateResult.Fail("Token no válido.");
        }

        ClaimsIdentity identidad = new(
            [
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre)
            ],
            Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(identidad),
            Scheme.Name));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        await Response.WriteAsJsonAsync(new { error = "Se requiere iniciar sesión." });
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json";
        await Response.WriteAsJsonAsync(new { error = "No tienes permiso para realizar esta operación." });
    }
}
