using System.Net.Mail;

namespace GestorTareas.ControlAcceso;

public sealed class ResultadoValidacionRegistro
{
    internal ResultadoValidacionRegistro(string correoNormalizado, IReadOnlyList<string> errores)
    {
        CorreoNormalizado = correoNormalizado;
        Errores = errores;
    }

    public bool EsValido => Errores.Count == 0;

    public string CorreoNormalizado { get; }

    public IReadOnlyList<string> Errores { get; }
}

public static class ValidadorRegistro
{
    public static ResultadoValidacionRegistro Validar(
        string? nombre,
        string? correo,
        string? contrasena)
    {
        var errores = new List<string>();
        string correoNormalizado = correo?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
        {
            errores.Add("El nombre es obligatorio.");
        }
        else if (nombre.Length > 100)
        {
            errores.Add("El nombre no debe superar los 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(correo))
        {
            errores.Add("El correo es obligatorio.");
        }
        else if (correoNormalizado.Length > 254)
        {
            errores.Add("El correo no debe superar los 254 caracteres.");
        }
        else if (!EsCorreoValido(correoNormalizado))
        {
            errores.Add("El correo no tiene un formato válido.");
        }

        errores.AddRange(ValidarContrasena(contrasena));

        return new ResultadoValidacionRegistro(correoNormalizado, errores.AsReadOnly());
    }

    public static ResultadoValidacionRegistro ValidarCorreo(string? correo)
    {
        var errores = new List<string>();
        string correoNormalizado = correo?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(correo))
        {
            errores.Add("El correo es obligatorio.");
        }
        else if (correoNormalizado.Length > 254)
        {
            errores.Add("El correo no debe superar los 254 caracteres.");
        }
        else if (!EsCorreoValido(correoNormalizado))
        {
            errores.Add("El correo no tiene un formato válido.");
        }

        return new ResultadoValidacionRegistro(correoNormalizado, errores.AsReadOnly());
    }

    public static ResultadoValidacionRegistro ValidarInicioSesion(
        string? correo,
        string? contrasena)
    {
        ResultadoValidacionRegistro validacionCorreo = ValidarCorreo(correo);
        var errores = validacionCorreo.Errores.ToList();

        if (string.IsNullOrEmpty(contrasena))
        {
            errores.Add("La contraseña es obligatoria.");
        }
        else if (contrasena.Length > 128)
        {
            errores.Add("La contraseña no debe superar los 128 caracteres.");
        }

        return new ResultadoValidacionRegistro(
            validacionCorreo.CorreoNormalizado,
            errores.AsReadOnly());
    }

    public static IReadOnlyList<string> ValidarContrasena(string? contrasena)
    {
        var errores = new List<string>();

        if (string.IsNullOrEmpty(contrasena))
        {
            errores.Add("La contraseña es obligatoria.");
        }
        else
        {
            if (contrasena.Length < 8)
            {
                errores.Add("La contraseña debe tener al menos 8 caracteres.");
            }

            if (contrasena.Length > 128)
            {
                errores.Add("La contraseña no debe superar los 128 caracteres.");
            }

            if (!contrasena.Any(char.IsLetter))
            {
                errores.Add("La contraseña debe incluir al menos una letra.");
            }

            if (!contrasena.Any(char.IsDigit))
            {
                errores.Add("La contraseña debe incluir al menos un número.");
            }
        }

        return errores.AsReadOnly();
    }

    private static bool EsCorreoValido(string correo)
    {
        try
        {
            var direccion = new MailAddress(correo);
            return string.Equals(direccion.Address, correo, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
