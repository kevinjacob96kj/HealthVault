using HealthVault.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthVault.Application.People;

/// <summary>
/// Resolves hospital scope for admins from HealthcareStaff assignments.
/// </summary>
internal static class HospitalStaffScope
{
    public static async Task<int> GetRequiredProviderIdAsync(
        AppDbContext context,
        string requestedByEmail,
        CancellationToken cancellationToken)
    {
        var email = requestedByEmail.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("You must be signed in to manage hospital staff.");
        }

        var providerId = await context.HealthcareStaff
            .AsNoTracking()
            .Where(staff => staff.Person.Email == email && staff.IsActive)
            .Select(staff => (int?)staff.HealthcareProviderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (providerId is null)
        {
            throw new UnauthorizedAccessException(
                "Your account is not assigned to a hospital, so you cannot manage staff.");
        }

        return providerId.Value;
    }

    public static async Task EnsurePersonIsAtProviderAsync(
        AppDbContext context,
        int providerId,
        int personId,
        CancellationToken cancellationToken)
    {
        var sameHospital = await context.HealthcareStaff
            .AsNoTracking()
            .AnyAsync(
                staff =>
                    staff.PersonId == personId &&
                    staff.HealthcareProviderId == providerId,
                cancellationToken);

        if (!sameHospital)
        {
            throw new UnauthorizedAccessException(
                "You can only view or change roles for staff at your hospital.");
        }
    }

    public static async Task<bool> GetIsActiveAsync(
        AppDbContext context,
        int personId,
        CancellationToken cancellationToken)
    {
        var isActive = await context.HealthcareStaff
            .AsNoTracking()
            .Where(staff => staff.PersonId == personId)
            .Select(staff => (bool?)staff.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        return isActive ?? true;
    }
}
