namespace HealthVault.Application.Auth;

/// <summary>
/// Short-lived verified identity for patient signup step 2.
/// </summary>
public class PatientSignupSessionStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(20);
    private readonly object _gate = new();
    private readonly Dictionary<string, SignupSession> _sessions = new(StringComparer.Ordinal);

    public string Create(string abhaId, string aadhaarNumber, DateOnly dateOfBirth)
    {
        var token = Guid.NewGuid().ToString("N");
        lock (_gate)
        {
            PruneExpiredUnlocked();
            _sessions[token] = new SignupSession(
                PatientAuthHelper.NormalizeAbhaId(abhaId),
                NormalizeAadhaar(aadhaarNumber),
                dateOfBirth,
                DateTime.UtcNow.Add(Lifetime));
        }

        return token;
    }

    public bool TryTake(
        string token,
        out string abhaId,
        out string aadhaarNumber,
        out DateOnly dateOfBirth)
    {
        abhaId = string.Empty;
        aadhaarNumber = string.Empty;
        dateOfBirth = default;

        lock (_gate)
        {
            PruneExpiredUnlocked();
            if (!_sessions.Remove(token.Trim(), out var session))
            {
                return false;
            }

            abhaId = session.AbhaId;
            aadhaarNumber = session.AadhaarNumber;
            dateOfBirth = session.DateOfBirth;
            return true;
        }
    }

    public bool TryPeek(
        string token,
        out string abhaId,
        out string aadhaarNumber,
        out DateOnly dateOfBirth)
    {
        abhaId = string.Empty;
        aadhaarNumber = string.Empty;
        dateOfBirth = default;

        lock (_gate)
        {
            PruneExpiredUnlocked();
            if (!_sessions.TryGetValue(token.Trim(), out var session))
            {
                return false;
            }

            abhaId = session.AbhaId;
            aadhaarNumber = session.AadhaarNumber;
            dateOfBirth = session.DateOfBirth;
            return true;
        }
    }

    public static string NormalizeAadhaar(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private void PruneExpiredUnlocked()
    {
        var now = DateTime.UtcNow;
        var expired = _sessions
            .Where(pair => pair.Value.ExpiresAtUtc <= now)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var key in expired)
        {
            _sessions.Remove(key);
        }
    }

    private sealed record SignupSession(
        string AbhaId,
        string AadhaarNumber,
        DateOnly DateOfBirth,
        DateTime ExpiresAtUtc);
}
