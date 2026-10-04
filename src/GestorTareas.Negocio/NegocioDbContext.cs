using GestorTareas.Negocio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Negocio;

public class NegocioDbContext(DbContextOptions<NegocioDbContext> options) : DbContext(options)
{
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<Prioridad> Prioridades => Set<Prioridad>();
    public DbSet<Tarea> Tareas => Set<Tarea>();
    public DbSet<Comentario> Comentarios => Set<Comentario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var proyecto = modelBuilder.Entity<Proyecto>();
        proyecto.ToTable("Proyectos");
        proyecto.HasKey(p => p.Id);
        proyecto.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(100);
        proyecto.HasMany(p => p.Tareas)
            .WithOne()
            .HasForeignKey(t => t.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        var prioridad = modelBuilder.Entity<Prioridad>();
        prioridad.ToTable("Prioridades");
        prioridad.HasKey(p => p.Id);
        prioridad.Property(p => p.Nombre)
            .IsRequired()
            .HasMaxLength(50);
        prioridad.HasMany(p => p.Tareas)
            .WithOne()
            .HasForeignKey(t => t.PrioridadId)
            .OnDelete(DeleteBehavior.Restrict);

        var tarea = modelBuilder.Entity<Tarea>();
        tarea.ToTable("Tareas");
        tarea.HasKey(t => t.Id);
        tarea.Property(t => t.Titulo)
            .IsRequired()
            .HasMaxLength(150);
        tarea.Property(t => t.Estado)
            .HasConversion<string>()
            .HasMaxLength(20);
        tarea.Property(t => t.MotivoCancelacion)
            .HasMaxLength(500);
        tarea.HasMany(t => t.Comentarios)
            .WithOne()
            .HasForeignKey(c => c.TareaId)
            .OnDelete(DeleteBehavior.Restrict);

        var comentario = modelBuilder.Entity<Comentario>();
        comentario.ToTable("Comentarios");
        comentario.HasKey(c => c.Id);
        comentario.Property(c => c.Texto)
            .IsRequired()
            .HasMaxLength(1000);
    }
}
