using GestorTareas.GestionPermisos.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.GestionPermisos;

public class ContextoGestionPermisos(DbContextOptions<ContextoGestionPermisos> opciones)
    : DbContext(opciones)
{
    public DbSet<SolicitudPermiso> SolicitudesPermiso => Set<SolicitudPermiso>();
    public DbSet<PermisoOtorgado> PermisosOtorgados => Set<PermisoOtorgado>();

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

        modelBuilder.Entity<PermisoOtorgado>(entidad =>
        {
            entidad.ToTable("PermisosOtorgados");

            entidad.HasKey(permisoOtorgado => permisoOtorgado.Id);

            entidad.Property(permisoOtorgado => permisoOtorgado.IdUsuario)
                .IsRequired();

            entidad.Property(permisoOtorgado => permisoOtorgado.Permiso)
                .HasConversion<int>()
                .IsRequired();

            entidad.Property(permisoOtorgado => permisoOtorgado.FechaOtorgamiento)
                .IsRequired();

            entidad.HasIndex(permisoOtorgado => new
                {
                    permisoOtorgado.IdUsuario,
                    permisoOtorgado.Permiso
                })
                .IsUnique();
        });
    }
}
