using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace HealthVault.Application.Auth;

/// <summary>
/// Stores short-lived patient login OTPs in memory.
/// </summary>
public class PatientOtpStore
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, OtpEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public string Create(string abhaId, string email)
    {
        var key = BuildKey(abhaId, email);
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _entries[key] = new OtpEntry(code, DateTimeOffset.UtcNow.Add(OtpLifetime));
        return code;
    }

    public bool TryValidate(string abhaId, string email, string otp)
    {
        var key = BuildKey(abhaId, email);
        if (!_entries.TryGetValue(key, out var entry))
        {
            return false;
        }

        if (entry.ExpiresAt < DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(key, out _);
            return false;
        }

        if (!string.Equals(entry.Code, otp.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        _entries.TryRemove(key, out _);
        return true;
    }

    private static string BuildKey(string abhaId, string email) =>
        $"{PatientAuthHelper.NormalizeAbhaId(abhaId)}|{email.Trim()}";

    private sealed record OtpEntry(string Code, DateTimeOffset ExpiresAt);
}
