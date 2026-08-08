using FoundationKit.Application.Abstractions;
using FoundationKit.Auditing;

namespace Madar.Api.Security;

public sealed class MadarAuditContextAccessor(
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : IAuditContextAccessor
{
    public AuditContext Current
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            return new AuditContext(
                currentUser.UserId?.ToString("D"),
                httpContext?.TraceIdentifier,
                null,
                "madar-api");
        }
    }
}
