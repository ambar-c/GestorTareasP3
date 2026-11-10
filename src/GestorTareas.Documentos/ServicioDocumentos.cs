using GestorTareas.Documentos.Entidades;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Documentos;

public sealed record ResultadoSubirDocumento(
    bool Exito,
    string? Error,
    Documento? Documento);

public enum EstadoOperacionDocumento
{
    Exitoso,
    NoEncontrado,
    SinPermiso,
    ArchivoNoDisponible
}

public sealed record ResultadoListarDocumentos(
    bool Exito,
    string? Error,
    List<Documento> Documentos);

public sealed record ResultadoDescargarDocumento(
    EstadoOperacionDocumento Estado,
    bool Exito,
    string? Error,
    Stream? Contenido,
    string? NombreOriginal,
    string? TipoContenido);

public sealed record ResultadoEliminarDocumento(
    EstadoOperacionDocumento Estado,
    bool Exito,
    string? Error,
    Documento? Documento);

public sealed class ServicioDocumentos(
    ContextoDocumentos contexto,
    OpcionesAlmacenamientoDocumentos opciones)
{
    private const long TamanoMaximoBytes = 10 * 1024 * 1024;

    public async Task<ResultadoSubirDocumento> SubirAsync(
        Guid idUsuario,
        string nombreOriginal,
        string tipoContenido,
        Stream contenido)
    {
        if (contenido is null)
        {
            return new ResultadoSubirDocumento(
                false,
                "Debes indicar el contenido del archivo.",
                null);
        }

        string nombreSinRuta = Path.GetFileName(
            (nombreOriginal ?? string.Empty).Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(nombreSinRuta))
        {
            return new ResultadoSubirDocumento(
                false,
                "Debes indicar el nombre del archivo.",
                null);
        }

        if (nombreSinRuta.Length > 255)
        {
            return new ResultadoSubirDocumento(
                false,
                "El nombre del archivo no puede superar los 255 caracteres.",
                null);
        }

        string tipoContenidoNormalizado = string.IsNullOrWhiteSpace(tipoContenido)
            ? "application/octet-stream"
            : tipoContenido;
        if (tipoContenidoNormalizado.Length > 100)
        {
            return new ResultadoSubirDocumento(
                false,
                "El tipo de contenido no puede superar los 100 caracteres.",
                null);
        }

        Directory.CreateDirectory(opciones.Ruta);

        string nombreAlmacenado = Guid.NewGuid().ToString("N");
        string rutaArchivo = Path.Combine(opciones.Ruta, nombreAlmacenado);
        long tamanoBytes = 0;

        try
        {
            await using (var archivo = new FileStream(
                rutaArchivo,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                byte[] buffer = new byte[81920];
                int bytesLeidos;
                while ((bytesLeidos = await contenido.ReadAsync(buffer)) > 0)
                {
                    tamanoBytes += bytesLeidos;
                    if (tamanoBytes > TamanoMaximoBytes)
                    {
                        archivo.Close();
                        File.Delete(rutaArchivo);
                        return new ResultadoSubirDocumento(
                            false,
                            "El archivo no puede superar los 10 MB.",
                            null);
                    }

                    await archivo.WriteAsync(buffer.AsMemory(0, bytesLeidos));
                }
            }

            if (tamanoBytes == 0)
            {
                File.Delete(rutaArchivo);
                return new ResultadoSubirDocumento(
                    false,
                    "El archivo no puede estar vacío.",
                    null);
            }

            var documento = new Documento
            {
                Id = Guid.NewGuid(),
                NombreOriginal = nombreSinRuta,
                TipoContenido = tipoContenidoNormalizado,
                TamanoBytes = tamanoBytes,
                FechaSubida = DateTime.UtcNow,
                IdUsuarioPropietario = idUsuario,
                NombreAlmacenado = nombreAlmacenado
            };

            contexto.Documentos.Add(documento);
            await contexto.SaveChangesAsync();

            return new ResultadoSubirDocumento(true, null, documento);
        }
        catch
        {
            if (File.Exists(rutaArchivo))
            {
                File.Delete(rutaArchivo);
            }

            throw;
        }
    }

    public async Task<ResultadoListarDocumentos> ListarAsync(
        Guid idUsuario,
        bool esAdministrador)
    {
        IQueryable<Documento> consulta = contexto.Documentos
            .AsNoTracking()
            .Where(documento => !documento.Eliminado);

        if (!esAdministrador)
        {
            consulta = consulta.Where(documento =>
                documento.IdUsuarioPropietario == idUsuario);
        }

        List<Documento> documentos = await consulta
            .OrderByDescending(documento => documento.FechaSubida)
            .ToListAsync();

        return new ResultadoListarDocumentos(true, null, documentos);
    }

    public async Task<ResultadoDescargarDocumento> DescargarAsync(
        Guid idUsuario,
        bool esAdministrador,
        Guid idDocumento)
    {
        Documento? documento = await contexto.Documentos
            .AsNoTracking()
            .SingleOrDefaultAsync(documento => documento.Id == idDocumento);

        if (documento is null || documento.Eliminado)
        {
            return new ResultadoDescargarDocumento(
                EstadoOperacionDocumento.NoEncontrado,
                false,
                "Documento no encontrado.",
                null,
                null,
                null);
        }

        if (!esAdministrador && documento.IdUsuarioPropietario != idUsuario)
        {
            return new ResultadoDescargarDocumento(
                EstadoOperacionDocumento.SinPermiso,
                false,
                "No tienes permiso para descargar este documento.",
                null,
                null,
                null);
        }

        string rutaArchivo = Path.Combine(opciones.Ruta, documento.NombreAlmacenado);
        if (!File.Exists(rutaArchivo))
        {
            return new ResultadoDescargarDocumento(
                EstadoOperacionDocumento.ArchivoNoDisponible,
                false,
                "El archivo del documento no está disponible.",
                null,
                null,
                null);
        }

        try
        {
            Stream contenido = new FileStream(
                rutaArchivo,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            return new ResultadoDescargarDocumento(
                EstadoOperacionDocumento.Exitoso,
                true,
                null,
                contenido,
                documento.NombreOriginal,
                documento.TipoContenido);
        }
        catch (FileNotFoundException)
        {
            return new ResultadoDescargarDocumento(
                EstadoOperacionDocumento.ArchivoNoDisponible,
                false,
                "El archivo del documento no está disponible.",
                null,
                null,
                null);
        }
        catch (DirectoryNotFoundException)
        {
            return new ResultadoDescargarDocumento(
                EstadoOperacionDocumento.ArchivoNoDisponible,
                false,
                "El archivo del documento no está disponible.",
                null,
                null,
                null);
        }
    }

    public async Task<ResultadoEliminarDocumento> EliminarAsync(
        Guid idUsuario,
        bool esAdministrador,
        Guid idDocumento)
    {
        Documento? documento = await contexto.Documentos
            .SingleOrDefaultAsync(documento => documento.Id == idDocumento);

        if (documento is null || documento.Eliminado)
        {
            return new ResultadoEliminarDocumento(
                EstadoOperacionDocumento.NoEncontrado,
                false,
                "Documento no encontrado.",
                null);
        }

        if (!esAdministrador && documento.IdUsuarioPropietario != idUsuario)
        {
            return new ResultadoEliminarDocumento(
                EstadoOperacionDocumento.SinPermiso,
                false,
                "No tienes permiso para eliminar este documento.",
                null);
        }

        documento.Eliminado = true;
        await contexto.SaveChangesAsync();

        return new ResultadoEliminarDocumento(
            EstadoOperacionDocumento.Exitoso,
            true,
            null,
            documento);
    }
}
