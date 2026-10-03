using Microsoft.EntityFrameworkCore;

namespace GestorTareas.ControlAcceso;

public class ContextoControlAcceso(DbContextOptions<ContextoControlAcceso> opciones)
    : DbContext(opciones)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entidad =>
        {
            entidad.ToTable("Usuarios");

            entidad.HasKey(usuario => usuario.Id);

            entidad.Property(usuario => usuario.Nombre)
                .IsRequired()
                .HasMaxLength(100);

            entidad.Property(usuario => usuario.Correo)
                .IsRequired()
                .HasMaxLength(254);

            entidad.HasIndex(usuario => usuario.Correo)
                .IsUnique();

            entidad.Property(usuario => usuario.HashContrasena)
                .IsRequired();

            entidad.Property(usuario => usuario.Sal)
                .IsRequired();

            entidad.Property(usuario => usuario.HashTokenActivacion)
                .HasMaxLength(32);

            entidad.HasIndex(usuario => usuario.HashTokenActivacion);

            entidad.Property(usuario => usuario.Rol)
                .HasConversion<int>()
                .IsRequired();

            entidad.Property(usuario => usuario.Activo)
                .IsRequired();

            entidad.Property(usuario => usuario.FechaCreacion)
                .IsRequired();
        });
    }
}
