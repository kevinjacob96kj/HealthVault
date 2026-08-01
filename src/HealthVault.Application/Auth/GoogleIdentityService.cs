using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace HealthVault.Application.Auth;

public record GoogleIdentity(string Email, string FirstName, string LastName);

public interface IGoogleIdentityService
{
    Task<GoogleIdentity> ValidateIdTokenAsync(string idToken, CancellationToken cancellationToken);
}

public class GoogleIdentityService : IGoogleIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleAuthOptions _options;

    public GoogleIdentityService(HttpClient httpClient, IOptions<GoogleAuthOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<GoogleIdentity> ValidateIdTokenAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException("Google sign-in is not configured.");
        }

        var response = await _httpClient.GetAsync(
            $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken.Trim())}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedAccessException("Google sign-in failed. Try again.");
        }

        var payload = await response.Content.ReadFromJsonAsync<GoogleTokenInfo>(cancellationToken)
            ?? throw new UnauthorizedAccessException("Google sign-in failed. Try again.");

        if (!string.Equals(payload.Aud, _options.ClientId, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(payload.Email) ||
            !string.Equals(payload.EmailVerified, "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Google sign-in could not be verified.");
        }

        var firstName = string.IsNullOrWhiteSpace(payload.GivenName)
            ? payload.Name?.Split(' ', 2).FirstOrDefault() ?? "Patient"
            : payload.GivenName;
        var lastName = string.IsNullOrWhiteSpace(payload.FamilyName)
            ? (payload.Name?.Split(' ', 2).Skip(1).FirstOrDefault() ?? "User")
            : payload.FamilyName;

        return new GoogleIdentity(payload.Email.Trim(), firstName.Trim(), lastName.Trim());
    }

    private sealed class GoogleTokenInfo
    {
        [JsonPropertyName("aud")]
        public string? Aud { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public string? EmailVerified { get; set; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
