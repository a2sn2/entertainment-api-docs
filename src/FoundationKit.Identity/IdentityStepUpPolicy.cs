namespace FoundationKit.Identity;

[Flags]
public enum IdentityStepUpFactor
{
    None = 0,
    Password = 1,
    MultiFactor = 2
}

public enum IdentitySensitiveOperation
{
    ChangePassword,
    SetupMultiFactor,
    DisableMultiFactor,
    RegenerateRecoveryCodes
}

public static class IdentityStepUpPolicy
{
    public static IdentityStepUpFactor RequiredFactors(IdentitySensitiveOperation operation) =>
        operation switch
        {
            IdentitySensitiveOperation.ChangePassword => IdentityStepUpFactor.Password,
            IdentitySensitiveOperation.SetupMultiFactor => IdentityStepUpFactor.Password,
            IdentitySensitiveOperation.DisableMultiFactor =>
                IdentityStepUpFactor.Password | IdentityStepUpFactor.MultiFactor,
            IdentitySensitiveOperation.RegenerateRecoveryCodes =>
                IdentityStepUpFactor.Password | IdentityStepUpFactor.MultiFactor,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

    public static bool IsSatisfied(
        IdentitySensitiveOperation operation,
        bool passwordVerified,
        bool multiFactorVerified)
    {
        var required = RequiredFactors(operation);

        if (required.HasFlag(IdentityStepUpFactor.Password) && !passwordVerified)
        {
            return false;
        }

        if (required.HasFlag(IdentityStepUpFactor.MultiFactor) && !multiFactorVerified)
        {
            return false;
        }

        return true;
    }
}
