using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace EntertainmentDocs.Admin.Infrastructure.Authentication;

public sealed class JwtAuthenticationStateProvider(IAccessTokenStore tokenStore) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStore.GetAsync();
        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(Anonymous);

        var principal = JwtClaimsParser.CreatePrincipal(token);
        if (principal.Identity?.IsAuthenticated != true || JwtClaimsParser.IsExpired(principal, DateTimeOffset.UtcNow))
        {
            await tokenStore.ClearAsync();
            return new AuthenticationState(Anonymous);
        }

        return new AuthenticationState(principal);
    }

    public async Task SetAuthenticatedAsync(string accessToken)
    {
        await tokenStore.SetAsync(accessToken);
        var principal = JwtClaimsParser.CreatePrincipal(accessToken);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public async Task SignOutAsync()
    {
        await tokenStore.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }
}
