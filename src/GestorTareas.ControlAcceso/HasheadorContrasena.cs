using System.Security.Cryptography;

namespace GestorTareas.ControlAcceso;

public static class HasheadorContrasena
{
    private const int Iteraciones = 100_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public static (byte[] Sal, byte[] Hash) Hashear(string contrasena)
    {
        ArgumentNullException.ThrowIfNull(contrasena);

        byte[] sal = RandomNumberGenerator.GetBytes(TamanoSal);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            contrasena,
            sal,
            Iteraciones,
            HashAlgorithmName.SHA256,
            TamanoHash);

        return (sal, hash);
    }

    public static bool Verificar(string contrasena, byte[] sal, byte[] hash)
    {
        ArgumentNullException.ThrowIfNull(contrasena);
        ArgumentNullException.ThrowIfNull(sal);
        ArgumentNullException.ThrowIfNull(hash);

        byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(
            contrasena,
            sal,
            Iteraciones,
            HashAlgorithmName.SHA256,
            TamanoHash);

        return CryptographicOperations.FixedTimeEquals(hashCalculado, hash);
    }
}
