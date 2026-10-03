using Microsoft.EntityFrameworkCore;

namespace GestorTareas.ControlAcceso;

public sealed class ServicioSesion(ContextoControlAcceso contexto)
{
    public const int MinutosVigencia = 480;

    public async Task<(string Token, DateTime Vencimiento)> CrearAsync(Guid idUsuario)
    {
        (string token, byte[] hash) = GeneradorTokenActivacion.Generar();
        DateTime emision = DateTime.UtcNow;
        DateTime vencimiento = emision.AddMinutes(MinutosVigencia);

        contexto.Sesiones.Add(new Sesion
        {
            Id = Guid.NewGuid(),
            HashToken = hash,
            UsuarioId = idUsuario,
            FechaEmision = emision,
            FechaVencimiento = vencimiento,
            Revocada = false
        });
        await contexto.SaveChangesAsync();

        return (token, vencimiento);
    }

    public Task<Usuario?> ObtenerUsuarioActualAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult<Usuario?>(null);
        }

        byte[] hash = GeneradorTokenActivacion.Hashear(token);
        DateTime ahora = DateTime.UtcNow;

        return contexto.Sesiones
            .Where(sesion => sesion.HashToken == hash
                && !sesion.Revocada
                && sesion.FechaVencimiento > ahora
                && sesion.Usuario.Activo)
            .Select(sesion => sesion.Usuario)
            .SingleOrDefaultAsync();
    }

    public async Task<bool> RevocarAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        byte[] hash = GeneradorTokenActivacion.Hashear(token);
        int filas = await contexto.Sesiones
            .Where(sesion => sesion.HashToken == hash && !sesion.Revocada)
            .ExecuteUpdateAsync(actualizacion => actualizacion
                .SetProperty(sesion => sesion.Revocada, true));

        return filas == 1;
    }

    public Task<int> RevocarPorUsuarioAsync(Guid idUsuario)
    {
        return contexto.Sesiones
            .Where(sesion => sesion.UsuarioId == idUsuario && !sesion.Revocada)
            .ExecuteUpdateAsync(actualizacion => actualizacion
                .SetProperty(sesion => sesion.Revocada, true));
    }
}
