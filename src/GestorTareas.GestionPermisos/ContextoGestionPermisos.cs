using GestorTareas.GestionPermisos.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.GestionPermisos;

public class ContextoGestionPermisos(DbContextOptions<ContextoGestionPermisos> opciones)
    : DbContext(opciones)
{
    public DbSet<SolicitudPermiso> SolicitudesPermiso => Set<SolicitudPermiso>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SolicitudPermiso>(entidad =>
        {
            entidad.ToTable("SolicitudesPermiso");

            entidad.HasKey(solicitud => solicitud.Id);

            entidad.Property(solicitud => solicitud.IdSolicitante)
                .IsRequired();

            entidad.Property(solicitud => solicitud.PermisoSolicitado)
                .HasConversion<int>()
                .IsRequired();

            entidad.Property(solicitud => solicitud.Estado)
                .HasConversion<int>()
                .IsRequired();

            entidad.Property(solicitud => solicitud.IdAprobador)
                .IsRequired(false);

            entidad.Property(solicitud => solicitud.FechaCreacion)
                .IsRequired();

            entidad.Property(solicitud => solicitud.FechaResolucion)
                .IsRequired(false);

            entidad.Property(solicitud => solicitud.MotivoRechazo)
                .IsRequired(false);
        });
    }
}
