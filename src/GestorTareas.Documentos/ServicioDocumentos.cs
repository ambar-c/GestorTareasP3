using GestorTareas.Documentos.Entidades;

namespace GestorTareas.Documentos;

public sealed record ResultadoSubirDocumento(
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
}
