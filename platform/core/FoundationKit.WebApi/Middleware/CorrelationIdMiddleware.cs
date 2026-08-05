using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FoundationKit.WebApi.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var supplied) &&
                            supplied.Count == 1 &&
                            !string.IsNullOrWhiteSpace(supplied[0]) &&
                            supplied[0]!.Length <= 128
            ? supplied[0]!
            : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
