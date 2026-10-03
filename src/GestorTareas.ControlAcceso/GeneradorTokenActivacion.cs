using System.Security.Cryptography;
using System.Text;

namespace GestorTareas.ControlAcceso;

public static class GeneradorTokenActivacion
{
    public static (string Token, byte[] Hash) Generar()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        string token = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return (token, Hashear(token));
    }

    public static byte[] Hashear(string token)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }
}
