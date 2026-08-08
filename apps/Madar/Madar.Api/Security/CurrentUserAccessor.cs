using System.Security.Claims;
using FoundationKit.Application.Abstractions;
using FoundationKit.Authorization;

namespace Madar.Api.Security;

public sealed class CurrentUserAccessor(
    IHttpContextAccessor httpContextAccessor) : ICurrentUser, IAuthorizationSubject
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(
            Principal?.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId)
            ? userId
            : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
