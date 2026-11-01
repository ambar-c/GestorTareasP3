using GestorTareas.GestionPermisos;
using GestorTareas.Negocio;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Pruebas.Integracion;

public class IntegracionNegocioPermisosPruebas
{
    [Fact]
    public async Task RF_NEG_06_CrearProyecto_EstandarAntesYDespuesDeAprobar_SeRechazaYLuegoSeCrea()
    {
        // Preparar
        string nombreBasePermisos = Guid.NewGuid().ToString();
        string nombreBaseNegocio = Guid.NewGuid().ToString();
        Guid idEstandar = Guid.NewGuid();
        Guid idAdministrador = Guid.NewGuid();

        using var contextoPermisos = CrearContextoPermisos(nombreBasePermisos);
        using var contextoNegocio = CrearContextoNegocio(nombreBaseNegocio);
        var servicioPermisos = new ServicioSolicitudesPermiso(contextoPermisos);
        var servicioProyectos = new ServicioProyectos(contextoNegocio, servicioPermisos);
        var solicitud = await servicioPermisos.CrearAsync(idEstandar, "CrearProyecto");
        Guid idSolicitud = solicitud.Solicitud!.Id;

        // Actuar
        var resultadoAntesDeAprobar = await servicioProyectos.CrearAsync(
            idEstandar,
            "Proyecto",
            false);

        // Verificar
        Assert.Equal(EstadoCrearProyecto.SinPermiso, resultadoAntesDeAprobar.Estado);
        Assert.Empty(await contextoNegocio.Proyectos.ToListAsync());

        // Actuar
        var resultadoAprobacion = await servicioPermisos.AprobarAsync(
            idSolicitud,
            idAdministrador);
        var resultadoDespuesDeAprobar = await servicioProyectos.CrearAsync(
            idEstandar,
            "Proyecto",
            false);

        // Verificar
        Assert.Equal(EstadoResolucionSolicitudPermiso.Exitoso, resultadoAprobacion.Estado);
        Assert.Equal(EstadoCrearProyecto.Exitoso, resultadoDespuesDeAprobar.Estado);
        var proyecto = Assert.Single(await contextoNegocio.Proyectos.ToListAsync());
        Assert.Equal(idEstandar, proyecto.UsuarioCreadorId);
    }

    [Fact]
    public async Task RF_NEG_06_CrearProyecto_SolicitudPendiente_SeRechazaSinPermiso()
    {
        // Preparar
        string nombreBasePermisos = Guid.NewGuid().ToString();
        string nombreBaseNegocio = Guid.NewGuid().ToString();
        Guid idEstandar = Guid.NewGuid();
        Guid idAdministrador = Guid.NewGuid();

        using var contextoPermisos = CrearContextoPermisos(nombreBasePermisos);
        using var contextoNegocio = CrearContextoNegocio(nombreBaseNegocio);
        var servicioPermisos = new ServicioSolicitudesPermiso(contextoPermisos);
        var servicioProyectos = new ServicioProyectos(contextoNegocio, servicioPermisos);
        await servicioPermisos.CrearAsync(idEstandar, "CrearProyecto");

        // Actuar
        var resultado = await servicioProyectos.CrearAsync(
            idEstandar,
            "Proyecto",
            false);

        // Verificar
        Assert.Equal(EstadoCrearProyecto.SinPermiso, resultado.Estado);
        Assert.Empty(await contextoNegocio.Proyectos.ToListAsync());
    }

    [Fact]
    public async Task RF_NEG_06_CrearProyecto_SolicitudRechazada_SigueSinPermiso()
    {
        // Preparar
        string nombreBasePermisos = Guid.NewGuid().ToString();
        string nombreBaseNegocio = Guid.NewGuid().ToString();
        Guid idEstandar = Guid.NewGuid();
        Guid idAdministrador = Guid.NewGuid();

        using var contextoPermisos = CrearContextoPermisos(nombreBasePermisos);
        using var contextoNegocio = CrearContextoNegocio(nombreBaseNegocio);
        var servicioPermisos = new ServicioSolicitudesPermiso(contextoPermisos);
        var servicioProyectos = new ServicioProyectos(contextoNegocio, servicioPermisos);
        var solicitud = await servicioPermisos.CrearAsync(idEstandar, "CrearProyecto");

        // Actuar
        var resultadoRechazo = await servicioPermisos.RechazarAsync(
            solicitud.Solicitud!.Id,
            idAdministrador,
            "No procede en este momento");
        var resultado = await servicioProyectos.CrearAsync(
            idEstandar,
            "Proyecto",
            false);

        // Verificar
        Assert.Equal(EstadoResolucionSolicitudPermiso.Exitoso, resultadoRechazo.Estado);
        Assert.Equal(EstadoCrearProyecto.SinPermiso, resultado.Estado);
        Assert.Empty(await contextoNegocio.Proyectos.ToListAsync());
    }

    private static ContextoGestionPermisos CrearContextoPermisos(string nombreBase)
    {
        var opciones = new DbContextOptionsBuilder<ContextoGestionPermisos>()
            .UseInMemoryDatabase(nombreBase)
            .Options;

        return new ContextoGestionPermisos(opciones);
    }

    private static NegocioDbContext CrearContextoNegocio(string nombreBase)
    {
        var opciones = new DbContextOptionsBuilder<NegocioDbContext>()
            .UseInMemoryDatabase(nombreBase)
            .Options;

        return new NegocioDbContext(opciones);
    }
}
