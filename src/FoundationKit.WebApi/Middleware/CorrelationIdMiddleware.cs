using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FoundationKit.WebApi.Middleware;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    private const int MaximumLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsValid(incoming)
            ? incoming!
            : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = correlationId
               }))
        {
            await next(context).ConfigureAwait(false);
        }
    }

    private static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaximumLength
        && value.All(character => !char.IsControl(character));
}
