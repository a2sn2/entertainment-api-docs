using Microsoft.AspNetCore.Http;

namespace FoundationKit.WebApi.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(static state =>
        {
            var response = (HttpResponse)state;
            response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            response.Headers.TryAdd("X-Frame-Options", "DENY");
            response.Headers.TryAdd("Referrer-Policy", "no-referrer");
            response.Headers.TryAdd(
                "Permissions-Policy",
                "camera=(), microphone=(), geolocation=()");
            return Task.CompletedTask;
        }, context.Response);

        await next(context).ConfigureAwait(false);
    }
}
