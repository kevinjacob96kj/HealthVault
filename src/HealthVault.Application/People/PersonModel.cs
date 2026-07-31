using System.Text.RegularExpressions;
using FluentValidation;

namespace HealthVault.Application.People;

public record PersonModel(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles);

internal static class PeopleRoles
{
    public const string DefaultPassword = "Password@1";
    public const string Admin = "Admin";
    public const string CentralAdmin = "CentralAdmin";
    public const string PasswordComplexityMessage =
        "Password must include at least 1 uppercase letter, 1 lowercase letter, 1 number, and 1 special character.";

    private static readonly Regex PasswordComplexity = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
        RegexOptions.Compiled);

    public static readonly IReadOnlyList<string> Allowed =
    [
        "Admin",
        "CentralAdmin",
        "Doctor",
        "Nurse",
        "Patient",
        "Staff"
    ];

    /// <summary>
    /// Roles a hospital admin may assign to staff at their provider.
    /// </summary>
    public static readonly IReadOnlyList<string> HospitalAssignable =
    [
        "Admin",
        "Doctor",
        "Nurse",
        "Staff"
    ];

    public static bool IsStrongPassword(string? password)
    {
        return !string.IsNullOrWhiteSpace(password) &&
               password.Length >= 8 &&
               PasswordComplexity.IsMatch(password);
    }

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Must(IsStrongPassword)
            .WithMessage(PasswordComplexityMessage);
    }

    public static bool AreValid(IEnumerable<string>? roles)
    {
        return (roles ?? []).All(role =>
            Allowed.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    public static bool AreHospitalAssignable(IEnumerable<string>? roles)
    {
        return (roles ?? []).All(role =>
            HospitalAssignable.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    public static List<string> Normalize(IEnumerable<string>? roles)
    {
        return NormalizeAgainst(roles, Allowed);
    }

    public static List<string> NormalizeHospitalAssignable(IEnumerable<string>? roles)
    {
        return NormalizeAgainst(roles, HospitalAssignable);
    }

    public static bool HasAdmin(IEnumerable<string>? roles)
    {
        return (roles ?? []).Any(role =>
            role.Equals(Admin, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> NormalizeAgainst(
        IEnumerable<string>? roles,
        IReadOnlyList<string> allowed)
    {
        return (roles ?? [])
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => allowed.First(item =>
                item.Equals(role.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
