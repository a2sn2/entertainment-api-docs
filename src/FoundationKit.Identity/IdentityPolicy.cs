namespace FoundationKit.Identity;

public sealed class IdentityPolicyOptions
{
    public const string SectionName = "AccountSecurity";

    public bool RequireConfirmedEmail { get; set; }

    public bool RequireAdministratorMfa { get; set; }

    public int PasswordRequiredLength { get; set; } = 10;

    public bool PasswordRequireDigit { get; set; } = true;

    public bool PasswordRequireLowercase { get; set; } = true;

    public bool PasswordRequireUppercase { get; set; } = true;

    public bool PasswordRequireNonAlphanumeric { get; set; } = true;
}

public static class IdentityPolicyValidator
{
    public static void Validate(IdentityPolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.PasswordRequiredLength is < 1 or > 128)
        {
            throw new InvalidOperationException(
                "Identity password required length must be between 1 and 128. The consuming product remains responsible for selecting its approved password policy within this supported range.");
        }
    }
}
