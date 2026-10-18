using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestorTareas.GestionPermisos;

public class FabricaContextoGestionPermisos : IDesignTimeDbContextFactory<ContextoGestionPermisos>
{
    public ContextoGestionPermisos CreateDbContext(string[] args)
    {
        var cadenaConexion = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "No se encontró la variable de entorno ConnectionStrings__Default con la cadena de conexión.");
        }

        var opciones = new DbContextOptionsBuilder<ContextoGestionPermisos>()
            .UseSqlServer(
                cadenaConexion,
                opcionesSql => opcionesSql.MigrationsHistoryTable("__EFMigrationsHistory_GestionPermisos"))
            .Options;

        return new ContextoGestionPermisos(opciones);
    }
}
