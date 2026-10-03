using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestorTareas.Notificaciones;

public class FabricaContextoNotificaciones : IDesignTimeDbContextFactory<ContextoNotificaciones>
{
    public ContextoNotificaciones CreateDbContext(string[] args)
    {
        var cadenaConexion = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "No se encontró la variable de entorno ConnectionStrings__Default.");
        }

        var opciones = new DbContextOptionsBuilder<ContextoNotificaciones>()
            .UseSqlServer(cadenaConexion, b =>
                b.MigrationsHistoryTable("__EFMigrationsHistory_Notificaciones"))
            .Options;

        return new ContextoNotificaciones(opciones);
    }
}
