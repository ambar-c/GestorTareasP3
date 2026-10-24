using GestorTareas.GestionPermisos;
using GestorTareas.GestionPermisos.Entidades;
using GestorTareas.GestionPermisos.Estados;
using GestorTareas.GestionPermisos.Permisos;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Pruebas.GestionPermisos;

public class ServicioSolicitudesPermisoPruebas
{
    [Fact]
    public async Task RF_GP_03_AprobarAsync_SolicitudPendiente_GuardaEstadoAprobadorYFechaResolucion()
    {
        // Preparar
        string nombreBase = Guid.NewGuid().ToString();
        Guid idSolicitud = Guid.NewGuid();
        Guid idSolicitante = Guid.NewGuid();
        Guid idAdministrador = Guid.NewGuid();
        DateTime antesDeAprobar;
        DateTime despuésDeAprobar;

        using (var contexto = CrearContexto(nombreBase))
        {
            contexto.SolicitudesPermiso.Add(new SolicitudPermiso
            {
                Id = idSolicitud,
                IdSolicitante = idSolicitante,
                PermisoSolicitado = Permiso.CrearProyecto,
                Estado = EstadoSolicitudPermiso.Pendiente,
                FechaCreacion = DateTime.UtcNow
            });
            await contexto.SaveChangesAsync();

            var servicio = new ServicioSolicitudesPermiso(contexto);

            // Actuar
            antesDeAprobar = DateTime.UtcNow;
            var resultado = await servicio.AprobarAsync(idSolicitud, idAdministrador);
            despuésDeAprobar = DateTime.UtcNow;

            // Verificar
            Assert.Equal(EstadoResolucionSolicitudPermiso.Exitoso, resultado.Estado);
        }

        using var contextoVerificacion = CrearContexto(nombreBase);
        var solicitudGuardada = await contextoVerificacion.SolicitudesPermiso
            .SingleAsync(solicitud => solicitud.Id == idSolicitud);

        Assert.Equal(EstadoSolicitudPermiso.Aplicada, solicitudGuardada.Estado);
        Assert.Equal(idAdministrador, solicitudGuardada.IdAprobador);
        Assert.InRange(
            solicitudGuardada.FechaResolucion!.Value,
            antesDeAprobar,
            despuésDeAprobar);
    }

    [Fact]
    public async Task RF_GP_05_AprobarAsync_SolicitudPendiente_OtorgaPermisoAlSolicitante()
    {
        // Preparar
        string nombreBase = Guid.NewGuid().ToString();
        Guid idSolicitud = Guid.NewGuid();
        Guid idSolicitante = Guid.NewGuid();
        Guid idAdministrador = Guid.NewGuid();

        using (var contexto = CrearContexto(nombreBase))
        {
            contexto.SolicitudesPermiso.Add(new SolicitudPermiso
            {
                Id = idSolicitud,
                IdSolicitante = idSolicitante,
                PermisoSolicitado = Permiso.CrearProyecto,
                Estado = EstadoSolicitudPermiso.Pendiente,
                FechaCreacion = DateTime.UtcNow
            });
            await contexto.SaveChangesAsync();

            var servicio = new ServicioSolicitudesPermiso(contexto);

            // Actuar
            await servicio.AprobarAsync(idSolicitud, idAdministrador);
        }

        // Verificar
        using var contextoVerificacion = CrearContexto(nombreBase);
        var servicioVerificacion = new ServicioSolicitudesPermiso(contextoVerificacion);
        bool tienePermiso = await servicioVerificacion.TienePermisoAsync(
            idSolicitante,
            Permiso.CrearProyecto);

        Assert.True(tienePermiso);
    }

    [Fact]
    public async Task RF_GP_04_RechazarAsync_SolicitudPendienteConMotivo_GuardaEstadoYMotivo()
    {
        // Preparar
        string nombreBase = Guid.NewGuid().ToString();
        Guid idSolicitud = Guid.NewGuid();
        Guid idSolicitante = Guid.NewGuid();
        Guid idAdministrador = Guid.NewGuid();
        const string motivo = "No cumple los requisitos";

        using (var contexto = CrearContexto(nombreBase))
        {
            contexto.SolicitudesPermiso.Add(new SolicitudPermiso
            {
                Id = idSolicitud,
                IdSolicitante = idSolicitante,
                PermisoSolicitado = Permiso.CrearProyecto,
                Estado = EstadoSolicitudPermiso.Pendiente,
                FechaCreacion = DateTime.UtcNow
            });
            await contexto.SaveChangesAsync();

            var servicio = new ServicioSolicitudesPermiso(contexto);

            // Actuar
            var resultado = await servicio.RechazarAsync(
                idSolicitud,
                idAdministrador,
                motivo);

            // Verificar
            Assert.Equal(EstadoResolucionSolicitudPermiso.Exitoso, resultado.Estado);
        }

        using var contextoVerificacion = CrearContexto(nombreBase);
        var solicitudGuardada = await contextoVerificacion.SolicitudesPermiso
            .SingleAsync(solicitud => solicitud.Id == idSolicitud);

        Assert.Equal(EstadoSolicitudPermiso.Rechazada, solicitudGuardada.Estado);
        Assert.Equal(motivo, solicitudGuardada.MotivoRechazo);
    }

    [Fact]
    public async Task RF_GP_06_AprobarAsync_SolicitudRechazada_DevuelveTransicionNoValidaYConservaEstado()
    {
        // Preparar
        string nombreBase = Guid.NewGuid().ToString();
        Guid idSolicitud = Guid.NewGuid();
        Guid idSolicitante = Guid.NewGuid();
        Guid idAdministrador = Guid.NewGuid();

        using (var contexto = CrearContexto(nombreBase))
        {
            contexto.SolicitudesPermiso.Add(new SolicitudPermiso
            {
                Id = idSolicitud,
                IdSolicitante = idSolicitante,
                PermisoSolicitado = Permiso.CrearProyecto,
                Estado = EstadoSolicitudPermiso.Rechazada,
                FechaCreacion = DateTime.UtcNow
            });
            await contexto.SaveChangesAsync();

            var servicio = new ServicioSolicitudesPermiso(contexto);

            // Actuar
            var resultado = await servicio.AprobarAsync(idSolicitud, idAdministrador);

            // Verificar
            Assert.Equal(EstadoResolucionSolicitudPermiso.TransicionNoValida, resultado.Estado);
        }

        using var contextoVerificacion = CrearContexto(nombreBase);
        var solicitudGuardada = await contextoVerificacion.SolicitudesPermiso
            .SingleAsync(solicitud => solicitud.Id == idSolicitud);

        Assert.Equal(EstadoSolicitudPermiso.Rechazada, solicitudGuardada.Estado);
    }

    [Fact]
    public async Task RF_GP_06_AprobarAsync_SolicitudAplicada_DevuelveTransicionNoValidaYConservaEstado()
    {
        // Preparar
        string nombreBase = Guid.NewGuid().ToString();
        Guid idSolicitud = Guid.NewGuid();
        Guid idSolicitante = Guid.NewGuid();
        Guid idAdministrador = Guid.NewGuid();

        using (var contexto = CrearContexto(nombreBase))
        {
            contexto.SolicitudesPermiso.Add(new SolicitudPermiso
            {
                Id = idSolicitud,
                IdSolicitante = idSolicitante,
                PermisoSolicitado = Permiso.CrearProyecto,
                Estado = EstadoSolicitudPermiso.Aplicada,
                FechaCreacion = DateTime.UtcNow
            });
            await contexto.SaveChangesAsync();

            var servicio = new ServicioSolicitudesPermiso(contexto);

            // Actuar
            var resultado = await servicio.AprobarAsync(idSolicitud, idAdministrador);

            // Verificar
            Assert.Equal(EstadoResolucionSolicitudPermiso.TransicionNoValida, resultado.Estado);
        }

        using var contextoVerificacion = CrearContexto(nombreBase);
        var solicitudGuardada = await contextoVerificacion.SolicitudesPermiso
            .SingleAsync(solicitud => solicitud.Id == idSolicitud);

        Assert.Equal(EstadoSolicitudPermiso.Aplicada, solicitudGuardada.Estado);
    }

    [Fact]
    public async Task RF_GP_04_RechazarAsync_MotivoConSoloEspacios_DevuelveMotivoInvalidoYConservaEstado()
    {
        // Preparar
        string nombreBase = Guid.NewGuid().ToString();
        Guid idSolicitud = Guid.NewGuid();
        Guid idSolicitante = Guid.NewGuid();
        Guid idAdministrador = Guid.NewGuid();

        using (var contexto = CrearContexto(nombreBase))
        {
            contexto.SolicitudesPermiso.Add(new SolicitudPermiso
            {
                Id = idSolicitud,
                IdSolicitante = idSolicitante,
                PermisoSolicitado = Permiso.CrearProyecto,
                Estado = EstadoSolicitudPermiso.Pendiente,
                FechaCreacion = DateTime.UtcNow
            });
            await contexto.SaveChangesAsync();

            var servicio = new ServicioSolicitudesPermiso(contexto);

            // Actuar
            var resultado = await servicio.RechazarAsync(
                idSolicitud,
                idAdministrador,
                "   ");

            // Verificar
            Assert.Equal(
                EstadoResolucionSolicitudPermiso.MotivoRechazoInvalido,
                resultado.Estado);
        }

        using var contextoVerificacion = CrearContexto(nombreBase);
        var solicitudGuardada = await contextoVerificacion.SolicitudesPermiso
            .SingleAsync(solicitud => solicitud.Id == idSolicitud);

        Assert.Equal(EstadoSolicitudPermiso.Pendiente, solicitudGuardada.Estado);
    }

    [Fact]
    public async Task RF_GP_07_AprobarAsync_AdministradorEsSolicitante_DevuelveMismoSolicitanteYConservaEstado()
    {
        // Preparar
        string nombreBase = Guid.NewGuid().ToString();
        Guid idSolicitud = Guid.NewGuid();
        Guid idAdministrador = Guid.NewGuid();

        using (var contexto = CrearContexto(nombreBase))
        {
            contexto.SolicitudesPermiso.Add(new SolicitudPermiso
            {
                Id = idSolicitud,
                IdSolicitante = idAdministrador,
                PermisoSolicitado = Permiso.CrearProyecto,
                Estado = EstadoSolicitudPermiso.Pendiente,
                FechaCreacion = DateTime.UtcNow
            });
            await contexto.SaveChangesAsync();

            var servicio = new ServicioSolicitudesPermiso(contexto);

            // Actuar
            var resultado = await servicio.AprobarAsync(idSolicitud, idAdministrador);

            // Verificar
            Assert.Equal(EstadoResolucionSolicitudPermiso.MismoSolicitante, resultado.Estado);
        }

        using var contextoVerificacion = CrearContexto(nombreBase);
        var solicitudGuardada = await contextoVerificacion.SolicitudesPermiso
            .SingleAsync(solicitud => solicitud.Id == idSolicitud);

        Assert.Equal(EstadoSolicitudPermiso.Pendiente, solicitudGuardada.Estado);
    }

    private static ContextoGestionPermisos CrearContexto(string nombreBase)
    {
        var opciones = new DbContextOptionsBuilder<ContextoGestionPermisos>()
            .UseInMemoryDatabase(nombreBase)
            .Options;

        return new ContextoGestionPermisos(opciones);
    }
}
