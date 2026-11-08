using GestorTareas.Documentos.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Documentos;

public class ContextoDocumentos(DbContextOptions<ContextoDocumentos> opciones)
    : DbContext(opciones)
{
    public DbSet<Documento> Documentos => Set<Documento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Documento>(entidad =>
        {
            entidad.ToTable("Documentos");

            entidad.HasKey(documento => documento.Id);

            entidad.Property(documento => documento.NombreOriginal)
                .HasMaxLength(255)
                .IsRequired();

            entidad.Property(documento => documento.TipoContenido)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(documento => documento.TamanoBytes)
                .IsRequired();

            entidad.Property(documento => documento.FechaSubida)
                .IsRequired();

            entidad.Property(documento => documento.IdUsuarioPropietario)
                .IsRequired();

            entidad.Property(documento => documento.NombreAlmacenado)
                .HasMaxLength(100)
                .IsRequired();

            entidad.Property(documento => documento.Eliminado)
                .IsRequired();

            entidad.HasIndex(documento => documento.IdUsuarioPropietario);
        });
    }
}
