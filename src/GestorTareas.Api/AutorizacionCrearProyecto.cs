using System.Security.Claims;
using GestorTareas.GestionPermisos;
using GestorTareas.GestionPermisos.Permisos;
using Microsoft.AspNetCore.Authorization;

namespace GestorTareas.Api;

public sealed class RequisitoCrearProyecto : IAuthorizationRequirement;

public sealed class ManejadorAutorizacionCrearProyecto(
    ServicioSolicitudesPermiso servicioSolicitudesPermiso)
    : AuthorizationHandler<RequisitoCrearProyecto>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext contexto,
        RequisitoCrearProyecto requisito)
    {
        if (contexto.User.IsInRole("Administrador"))
        {
            contexto.Succeed(requisito);
            return;
        }

        if (!Guid.TryParse(
                contexto.User.FindFirstValue(ClaimTypes.NameIdentifier),
                out Guid idUsuario))
        {
            return;
        }

        if (await servicioSolicitudesPermiso.TienePermisoAsync(
                idUsuario,
                Permiso.CrearProyecto))
        {
            contexto.Succeed(requisito);
        }
    }
}
