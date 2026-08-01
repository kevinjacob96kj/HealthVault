namespace HealthVault.Web.Authentication;

public static class HealthVaultPolicies
{
    public const string MustBeAnAdmin = "MustBeAnAdmin";
    public const string MustBeACentralAdmin = "MustBeACentralAdmin";
    public const string MustBeADoctor = "MustBeADoctor";
    public const string MustBeAPatient = "MustBeAPatient";
}
