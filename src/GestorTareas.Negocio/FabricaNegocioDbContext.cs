using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestorTareas.Negocio;

public class FabricaNegocioDbContext : IDesignTimeDbContextFactory<NegocioDbContext>
{
    public NegocioDbContext CreateDbContext(string[] args)
    {
        var cadenaConexion = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "No se encontró la variable de entorno ConnectionStrings__Default.");
        }

        var opciones = new DbContextOptionsBuilder<NegocioDbContext>()
            .UseSqlServer(cadenaConexion, b =>
                b.MigrationsHistoryTable("__EFMigrationsHistory_Negocio"))
            .Options;

        return new NegocioDbContext(opciones);
    }
}
