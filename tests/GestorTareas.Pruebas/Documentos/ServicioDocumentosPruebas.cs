using System.Text;
using GestorTareas.Documentos;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestorTareas.Pruebas.Documentos;

public sealed class ServicioDocumentosPruebas : IDisposable
{
    private readonly ContextoDocumentos _contexto;
    private readonly ServicioDocumentos _servicio;
    private readonly string _carpetaTemporal;

    public ServicioDocumentosPruebas()
    {
        // Una base de datos diferente para cada prueba.
        var opciones = new DbContextOptionsBuilder<ContextoDocumentos>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _contexto = new ContextoDocumentos(opciones);

        // Carpeta temporal aislada de los archivos reales.
        _carpetaTemporal = Path.Combine(
            Path.GetTempPath(),
            "GestorTareasPruebas",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_carpetaTemporal);

        var almacenamiento =
            new OpcionesAlmacenamientoDocumentos(_carpetaTemporal);

        _servicio = new ServicioDocumentos(
            _contexto,
            almacenamiento);
    }

    [Fact]
    public async Task SubirAsync_ArchivoValido_GuardaMetadatos()
    {
        // ARRANGE: preparar los datos de prueba.
        Guid idUsuario = Guid.NewGuid();
        string nombre = "documento-prueba.txt";
        string tipo = "text/plain";
        byte[] contenido = Encoding.UTF8.GetBytes(
            "Este es un documento de prueba.");

        await using var archivo = new MemoryStream(contenido);

        DateTime inicio = DateTime.UtcNow;

        // ACT: ejecutar la operación que queremos probar.
        var resultado = await _servicio.SubirAsync(
            idUsuario,
            nombre,
            tipo,
            archivo);

        DateTime fin = DateTime.UtcNow;

        // ASSERT: comprobar el resultado.
        Assert.True(resultado.Exito);
        Assert.Null(resultado.Error);
        Assert.NotNull(resultado.Documento);

        var documento = resultado.Documento!;

        Assert.Equal(nombre, documento.NombreOriginal);
        Assert.Equal(tipo, documento.TipoContenido);
        Assert.Equal(contenido.Length, documento.TamanoBytes);
        Assert.Equal(idUsuario, documento.IdUsuarioPropietario);

        Assert.InRange(documento.FechaSubida, inicio, fin);
        Assert.False(documento.Eliminado);

        // Comprobar que el registro quedó en la base.
        Assert.Equal(1, await _contexto.Documentos.CountAsync());

        // Comprobar que el archivo existe físicamente.
        string ruta = Path.Combine(
            _carpetaTemporal,
            documento.NombreAlmacenado);

        Assert.True(File.Exists(ruta));

        byte[] contenidoGuardado =
            await File.ReadAllBytesAsync(ruta);

        Assert.Equal(contenido, contenidoGuardado);
    }


    [Fact]
    public async Task SubirAsync_ArchivoVacio_RechazaYNoGuarda()
    {
        // ARRANGE: preparar un archivo sin contenido.
        Guid idUsuario = Guid.NewGuid();

        await using var archivo = new MemoryStream();

        // ACT: intentar subir el archivo vacío.
        var resultado = await _servicio.SubirAsync(
            idUsuario,
            "archivo-vacio.txt",
            "text/plain",
            archivo);

        // ASSERT: comprobar que se rechazó.
        Assert.False(resultado.Exito);
        Assert.NotNull(resultado.Error);
        Assert.Null(resultado.Documento);

        // No debe quedar ningún registro en la base.
        Assert.Equal(
            0,
            await _contexto.Documentos.CountAsync());

        // Tampoco debe quedar un archivo físico.
        Assert.Empty(Directory.GetFiles(_carpetaTemporal));
    }


    [Fact]
    public async Task ListarAsync_UsuarioEstandar_SoloVeSusDocumentos()
    {
        // ARRANGE: crear dos usuarios diferentes.
        Guid usuarioA = Guid.NewGuid();
        Guid usuarioB = Guid.NewGuid();

        await using var archivoA = new MemoryStream(
            Encoding.UTF8.GetBytes("Documento de A"));

        await using var archivoB = new MemoryStream(
            Encoding.UTF8.GetBytes("Documento de B"));

        await _servicio.SubirAsync(
            usuarioA, "archivo-A.txt", "text/plain", archivoA);

        await _servicio.SubirAsync(
            usuarioB, "archivo-B.txt", "text/plain", archivoB);

        // ACT: el usuario A consulta los documentos.
        var resultado = await _servicio.ListarAsync(
            usuarioA,
            esAdministrador: false);

        // ASSERT: solamente debe ver su archivo.
        Assert.True(resultado.Exito);
        Assert.Single(resultado.Documentos);
        Assert.Equal(
            "archivo-A.txt",
            resultado.Documentos[0].NombreOriginal);

        Assert.Equal(
            usuarioA,
            resultado.Documentos[0].IdUsuarioPropietario);
    }


    [Fact]
    public async Task ListarAsync_Administrador_VeDocumentosDeTodos()
    {
        // ARRANGE: crear documentos de dos propietarios.
        Guid usuarioA = Guid.NewGuid();
        Guid usuarioB = Guid.NewGuid();
        Guid administrador = Guid.NewGuid();

        await using var archivoA = new MemoryStream(
            Encoding.UTF8.GetBytes("Documento del usuario A"));

        await using var archivoB = new MemoryStream(
            Encoding.UTF8.GetBytes("Documento del usuario B"));

        await _servicio.SubirAsync(
            usuarioA, "archivo-A.txt", "text/plain", archivoA);

        await _servicio.SubirAsync(
            usuarioB, "archivo-B.txt", "text/plain", archivoB);

        // ACT: el Administrador consulta los documentos.
        var resultado = await _servicio.ListarAsync(
            administrador,
            esAdministrador: true);

        // ASSERT: debe encontrar ambos documentos.
        Assert.True(resultado.Exito);
        Assert.Equal(2, resultado.Documentos.Count);

        Assert.Contains(
            resultado.Documentos,
            documento => documento.IdUsuarioPropietario == usuarioA);

        Assert.Contains(
            resultado.Documentos,
            documento => documento.IdUsuarioPropietario == usuarioB);
    }


    [Fact]
    public async Task DescargarAsync_Propietario_DescargaCorrectamente()
    {
        // ARRANGE
        Guid usuario = Guid.NewGuid();
        byte[] contenido = Encoding.UTF8.GetBytes("Documento privado");

        await using var archivo = new MemoryStream(contenido);

        var subida = await _servicio.SubirAsync(
            usuario, "privado.txt", "text/plain", archivo);

        Assert.True(subida.Exito);
        Assert.NotNull(subida.Documento);

        // ACT
        var resultado = await _servicio.DescargarAsync(
            usuario,
            esAdministrador: false,
            subida.Documento!.Id);

        // ASSERT
        Assert.True(resultado.Exito);
        Assert.Equal(
            EstadoOperacionDocumento.Exitoso,
            resultado.Estado);

        Assert.Equal("privado.txt", resultado.NombreOriginal);
        Assert.Equal("text/plain", resultado.TipoContenido);
        Assert.NotNull(resultado.Contenido);

        await using var descarga = resultado.Contenido!;
        using var memoria = new MemoryStream();

        await descarga.CopyToAsync(memoria);

        Assert.Equal(contenido, memoria.ToArray());
    }


    [Fact]
    public async Task DescargarAsync_UsuarioAjeno_DevuelveSinPermiso()
    {
        // ARRANGE
        Guid propietario = Guid.NewGuid();
        Guid usuarioAjeno = Guid.NewGuid();

        await using var archivo = new MemoryStream(
            Encoding.UTF8.GetBytes("Archivo confidencial"));

        var subida = await _servicio.SubirAsync(
            propietario, "confidencial.txt", "text/plain", archivo);

        Assert.True(subida.Exito);

        // ACT
        var resultado = await _servicio.DescargarAsync(
            usuarioAjeno,
            esAdministrador: false,
            subida.Documento!.Id);

        // ASSERT
        Assert.False(resultado.Exito);
        Assert.Equal(
            EstadoOperacionDocumento.SinPermiso,
            resultado.Estado);

        Assert.Null(resultado.Contenido);
    }


    [Fact]
    public async Task DescargarAsync_ArchivoAusente_DevuelveRechazoControlado()
    {
        // ARRANGE
        Guid usuario = Guid.NewGuid();

        await using var archivo = new MemoryStream(
            Encoding.UTF8.GetBytes("Documento temporal"));

        var subida = await _servicio.SubirAsync(
            usuario, "temporal.txt", "text/plain", archivo);

        Assert.True(subida.Exito);
        Assert.NotNull(subida.Documento);

        // Simulamos que el archivo desapareció del disco.
        string ruta = Path.Combine(
            _carpetaTemporal,
            subida.Documento!.NombreAlmacenado);

        File.Delete(ruta);

        // ACT
        var resultado = await _servicio.DescargarAsync(
            usuario,
            esAdministrador: false,
            subida.Documento.Id);

        // ASSERT
        Assert.False(resultado.Exito);
        Assert.Equal(
            EstadoOperacionDocumento.ArchivoNoDisponible,
            resultado.Estado);

        Assert.Null(resultado.Contenido);
        Assert.NotNull(resultado.Error);

        // El registro en la base de datos debe conservarse.
        Assert.Equal(1, await _contexto.Documentos.CountAsync());
    }


    [Fact]
    public async Task EliminarAsync_Propietario_RealizaEliminacionLogica()
    {
        // ARRANGE
        Guid usuario = Guid.NewGuid();

        await using var archivo = new MemoryStream(
            Encoding.UTF8.GetBytes("Documento para eliminar"));

        var subida = await _servicio.SubirAsync(
            usuario, "eliminar.txt", "text/plain", archivo);

        Assert.True(subida.Exito);
        Assert.NotNull(subida.Documento);

        Guid idDocumento = subida.Documento!.Id;

        string ruta = Path.Combine(
            _carpetaTemporal,
            subida.Documento.NombreAlmacenado);

        // ACT
        var resultado = await _servicio.EliminarAsync(
            usuario,
            esAdministrador: false,
            idDocumento);

        // ASSERT
        Assert.True(resultado.Exito);
        Assert.Equal(
            EstadoOperacionDocumento.Exitoso,
            resultado.Estado);

        // El registro permanece, pero marcado como eliminado.
        var documentoGuardado = await _contexto.Documentos
            .SingleAsync(d => d.Id == idDocumento);

        Assert.True(documentoGuardado.Eliminado);

        // El archivo físico no se borra.
        Assert.True(File.Exists(ruta));
    }


    [Fact]
    public async Task EliminarAsync_UsuarioAjeno_DevuelveSinPermiso()
    {
        // ARRANGE
        Guid propietario = Guid.NewGuid();
        Guid usuarioAjeno = Guid.NewGuid();

        await using var archivo = new MemoryStream(
            Encoding.UTF8.GetBytes("Documento privado"));

        var subida = await _servicio.SubirAsync(
            propietario, "privado.txt", "text/plain", archivo);

        Assert.True(subida.Exito);
        Assert.NotNull(subida.Documento);

        Guid idDocumento = subida.Documento!.Id;

        // ACT
        var resultado = await _servicio.EliminarAsync(
            usuarioAjeno,
            esAdministrador: false,
            idDocumento);

        // ASSERT
        Assert.False(resultado.Exito);
        Assert.Equal(
            EstadoOperacionDocumento.SinPermiso,
            resultado.Estado);

        // El documento no debe modificarse.
        var documentoGuardado = await _contexto.Documentos
            .SingleAsync(d => d.Id == idDocumento);

        Assert.False(documentoGuardado.Eliminado);

        // El propietario todavía puede verlo.
        var listado = await _servicio.ListarAsync(
            propietario,
            esAdministrador: false);

        Assert.Single(listado.Documentos);
    }


    [Fact]
    public async Task ListarAsync_DocumentoEliminado_NoApareceEnListado()
    {
        // ARRANGE
        Guid usuario = Guid.NewGuid();

        await using var archivo = new MemoryStream(
            Encoding.UTF8.GetBytes("Documento de prueba"));

        var subida = await _servicio.SubirAsync(
            usuario, "eliminado.txt", "text/plain", archivo);

        Assert.True(subida.Exito);
        Assert.NotNull(subida.Documento);

        Guid idDocumento = subida.Documento!.Id;

        var eliminacion = await _servicio.EliminarAsync(
            usuario,
            esAdministrador: false,
            idDocumento);

        Assert.True(eliminacion.Exito);

        // ACT
        var listado = await _servicio.ListarAsync(
            usuario,
            esAdministrador: false);

        // ASSERT
        Assert.True(listado.Exito);
        Assert.Empty(listado.Documentos);

        // El Administrador tampoco debe verlo en el listado.
        var listadoAdministrador = await _servicio.ListarAsync(
            Guid.NewGuid(),
            esAdministrador: true);

        Assert.Empty(listadoAdministrador.Documentos);

        // El registro continúa en la base de datos.
        Assert.Equal(
            1,
            await _contexto.Documentos.CountAsync());
    }


    public void Dispose()
    {
        _contexto.Dispose();

        // Limpiar únicamente la carpeta temporal de esta prueba.
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }
}
