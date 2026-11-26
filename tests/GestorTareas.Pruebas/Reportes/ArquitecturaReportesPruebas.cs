namespace GestorTareas.Pruebas.Reportes;

public class ArquitecturaReportesPruebas
{
    [Fact]
    public void H1_Program_no_contiene_consultas_de_reportes()
    {
        // H1: la lógica de reportes no debe vivir dentro de Program.cs.
        // Preparar
        DirectoryInfo? directorioRepositorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorioRepositorio is not null &&
               !File.Exists(Path.Combine(directorioRepositorio.FullName, "GestorTareas.slnx")))
        {
            directorioRepositorio = directorioRepositorio.Parent;
        }

        Assert.NotNull(directorioRepositorio);
        string rutaProgram = Path.Combine(
            directorioRepositorio!.FullName,
            "src",
            "GestorTareas.Api",
            "Program.cs");

        // Actuar
        string contenidoProgram = File.ReadAllText(rutaProgram);

        // Verificar
        Assert.DoesNotContain(".GroupBy(", contenidoProgram);
        Assert.DoesNotContain("SolicitudesPermiso.AsNoTracking", contenidoProgram);
        Assert.DoesNotContain("Tareas.AsNoTracking", contenidoProgram);
    }
}
