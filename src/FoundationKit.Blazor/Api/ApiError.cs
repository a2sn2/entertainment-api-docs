using System.Net;

namespace FoundationKit.Blazor.Api;

public sealed record ApiError(
    string Code,
    string Message,
    HttpStatusCode? StatusCode = null,
    string? CorrelationId = null);
