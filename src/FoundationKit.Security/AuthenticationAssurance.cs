using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace FoundationKit.Security;

public static class FoundationAuthenticationAssurance
{
    public const string AuthenticationMethodClaimType = "amr";
    public const string MultiFactorAuthenticationMethod = "mfa";

    public static bool HasMultiFactorAuthentication(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return principal.Claims.Any(claim =>
            string.Equals(claim.Type, AuthenticationMethodClaimType, StringComparison.Ordinal)
            && string.Equals(claim.Value, MultiFactorAuthenticationMethod, StringComparison.Ordinal));
    }
}

public static class FoundationAuthorizationPolicyExtensions
{
    public static AuthorizationPolicyBuilder RequireFoundationMultiFactor(
        this AuthorizationPolicyBuilder policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return policy.RequireClaim(
            FoundationAuthenticationAssurance.AuthenticationMethodClaimType,
            FoundationAuthenticationAssurance.MultiFactorAuthenticationMethod);
    }
}
