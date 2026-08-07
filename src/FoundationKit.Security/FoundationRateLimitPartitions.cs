using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace FoundationKit.Security;

public static class FoundationRateLimitPartitions
{
    public static string Authentication(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return RemoteAddress(context);
    }

    public static string Write(
        HttpContext context,
        string userIdClaimType = ClaimTypes.NameIdentifier)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(userIdClaimType);

        var userId = context.User.FindFirstValue(userIdClaimType);
        return string.IsNullOrWhiteSpace(userId)
            ? RemoteAddress(context)
            : $"user:{userId}";
    }

    public static string RemoteAddress(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
