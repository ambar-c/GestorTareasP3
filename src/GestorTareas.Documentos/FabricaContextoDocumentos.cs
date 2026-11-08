using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestorTareas.Documentos;

public class FabricaContextoDocumentos : IDesignTimeDbContextFactory<ContextoDocumentos>
{
    public ContextoDocumentos CreateDbContext(string[] args)
    {
        var cadenaConexion = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "No se encontró la variable de entorno ConnectionStrings__Default con la cadena de conexión.");
        }

        var opciones = new DbContextOptionsBuilder<ContextoDocumentos>()
            .UseSqlServer(
                cadenaConexion,
                opcionesSql => opcionesSql.MigrationsHistoryTable("__EFMigrationsHistory_Documentos"))
            .Options;

        return new ContextoDocumentos(opciones);
    }
}
