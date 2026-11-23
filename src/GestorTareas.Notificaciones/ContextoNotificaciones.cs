using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Notificaciones;

public class ContextoNotificaciones(DbContextOptions<ContextoNotificaciones> options) : DbContext(options)
{
    public DbSet<CorreoEnCola> CorreosEnCola => Set<CorreoEnCola>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<PreferenciaNotificacion> PreferenciasNotificacion => Set<PreferenciaNotificacion>();

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

        var notificacion = modelBuilder.Entity<Notificacion>();
        notificacion.ToTable("Notificaciones");
        notificacion.HasKey(item => item.Id);
        notificacion.Property(item => item.UsuarioId).IsRequired();
        notificacion.Property(item => item.Titulo)
            .IsRequired()
            .HasMaxLength(200);
        notificacion.Property(item => item.Mensaje).IsRequired();
        notificacion.Property(item => item.Leida)
            .HasDefaultValue(false);
        notificacion.Property(item => item.FechaCreacion).IsRequired();
        notificacion.HasIndex(item => new { item.UsuarioId, item.Leida, item.FechaCreacion });

        var preferencia = modelBuilder.Entity<PreferenciaNotificacion>();
        preferencia.ToTable("PreferenciasNotificacion");
        preferencia.HasKey(item => item.UsuarioId);
    }
}
