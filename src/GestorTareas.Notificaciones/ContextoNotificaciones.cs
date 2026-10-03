using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Notificaciones;

public class ContextoNotificaciones(DbContextOptions<ContextoNotificaciones> options) : DbContext(options)
{
    public DbSet<CorreoEnCola> CorreosEnCola => Set<CorreoEnCola>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entidad = modelBuilder.Entity<CorreoEnCola>();

        entidad.ToTable("CorreosEnCola");
        entidad.HasKey(correo => correo.Id);
        entidad.Property(correo => correo.Destinatario)
            .IsRequired()
            .HasMaxLength(254);
        entidad.Property(correo => correo.Asunto)
            .IsRequired()
            .HasMaxLength(200);
        entidad.Property(correo => correo.Cuerpo)
            .IsRequired();
        entidad.Property(correo => correo.Estado)
            .HasConversion<int>();
        entidad.Property(correo => correo.UltimoError)
            .HasMaxLength(500);
        entidad.HasIndex(correo => new { correo.Estado, correo.FechaCreacion });
    }
}
