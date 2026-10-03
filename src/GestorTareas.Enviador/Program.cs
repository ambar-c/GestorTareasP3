using System.Net;
using System.Net.Mail;
using System.Text;
using GestorTareas.Notificaciones;
using Microsoft.EntityFrameworkCore;

return await Programa.MainAsync();

internal static class Programa
{
    private static readonly string[] VariablesRequeridas =
    [
        "SMTP_HOST",
        "SMTP_PUERTO",
        "SMTP_USUARIO",
        "SMTP_CLAVE",
        "SMTP_REMITENTE",
        "SMTP_NOMBRE_REMITENTE",
        "ConnectionStrings__Default"
    ];

    public static async Task<int> MainAsync()
    {
        var valores = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var nombre in VariablesRequeridas)
        {
            var valor = Environment.GetEnvironmentVariable(nombre);
            if (string.IsNullOrWhiteSpace(valor))
            {
                Console.WriteLine($"Falta la variable de entorno: {nombre}");
                return 1;
            }

            valores[nombre] = valor;
        }

        if (!int.TryParse(valores["SMTP_PUERTO"], out var puerto))
        {
            Console.WriteLine("La variable de entorno SMTP_PUERTO debe ser un entero.");
            return 1;
        }

        try
        {
            var opciones = new DbContextOptionsBuilder<ContextoNotificaciones>()
                .UseSqlServer(
                    valores["ConnectionStrings__Default"],
                    b => b.MigrationsHistoryTable("__EFMigrationsHistory_Notificaciones"))
                .Options;

            await using var contexto = new ContextoNotificaciones(opciones);
            var resultado = await ProcesarCorreosAsync(
                contexto,
                valores["SMTP_HOST"],
                puerto,
                valores["SMTP_USUARIO"],
                valores["SMTP_CLAVE"],
                valores["SMTP_REMITENTE"],
                valores["SMTP_NOMBRE_REMITENTE"]);

            Console.WriteLine($"Resumen: enviados {resultado.Enviados}; fallidos {resultado.Fallidos}.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al iniciar el Enviador: {ex.GetType().Name}.");
            return 1;
        }
    }

    private static async Task<Resultado> ProcesarCorreosAsync(
        ContextoNotificaciones contexto,
        string host,
        int puerto,
        string usuario,
        string clave,
        string remitente,
        string nombreRemitente)
    {
        var correos = await contexto.CorreosEnCola
            .Where(correo => correo.Estado == EstadoCorreo.Pendiente)
            .OrderBy(correo => correo.FechaCreacion)
            .ToListAsync();

        var enviados = 0;
        var fallidos = 0;

        using var cliente = new SmtpClient(host, puerto)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(usuario, clave),
            Timeout = 15000
        };

        foreach (var correo in correos)
        {
            try
            {
                using var mensaje = new MailMessage
                {
                    From = new MailAddress(remitente, nombreRemitente, Encoding.UTF8),
                    Subject = correo.Asunto,
                    Body = correo.Cuerpo,
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8,
                    IsBodyHtml = false
                };
                mensaje.To.Add(new MailAddress(correo.Destinatario));
                await cliente.SendMailAsync(mensaje);

                correo.Estado = EstadoCorreo.Enviado;
                correo.FechaEnvio = DateTime.UtcNow;
                correo.Intentos++;
                await contexto.SaveChangesAsync();
                enviados++;
            }
            catch (Exception ex)
            {
                correo.Estado = EstadoCorreo.Pendiente;
                correo.Intentos++;
                correo.UltimoError = ex.GetType().Name[..Math.Min(500, ex.GetType().Name.Length)];

                try
                {
                    await contexto.SaveChangesAsync();
                }
                catch
                {
                    // El correo siguiente debe intentarse aunque falle este guardado.
                }

                fallidos++;
            }
        }

        return new Resultado(enviados, fallidos);
    }

    private sealed record Resultado(int Enviados, int Fallidos);
}
