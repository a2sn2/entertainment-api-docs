using System.Security.Claims;
using EntertainmentDocs.Application.Abstractions;

namespace EntertainmentDocs.Api.Services;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
