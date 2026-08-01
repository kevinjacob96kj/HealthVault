namespace HealthVault.Application.Auth;

/// <summary>
/// Shared helpers for patient ABHA authentication.
/// </summary>
public static class PatientAuthHelper
{
    public static readonly HashSet<string> PatientRole = new(StringComparer.OrdinalIgnoreCase)
    {
        "Patient"
    };

    public static string NormalizeAbhaId(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 14)
        {
            return value.Trim();
        }

        return $"{digits[..2]}-{digits[2..6]}-{digits[6..10]}-{digits[10..14]}";
    }

    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return email;
        }

        return $"{email[0]}***{email[at..]}";
    }
}
