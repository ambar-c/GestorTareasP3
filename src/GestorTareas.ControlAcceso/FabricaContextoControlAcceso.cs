using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestorTareas.ControlAcceso;

public class FabricaContextoControlAcceso : IDesignTimeDbContextFactory<ContextoControlAcceso>
{
    public ContextoControlAcceso CreateDbContext(string[] args)
    {
        var cadenaConexion = Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "No se encontró la variable de entorno ConnectionStrings__Default con la cadena de conexión.");
        }

        var opciones = new DbContextOptionsBuilder<ContextoControlAcceso>()
            .UseSqlServer(cadenaConexion)
            .Options;

        return new ContextoControlAcceso(opciones);
    }
}
