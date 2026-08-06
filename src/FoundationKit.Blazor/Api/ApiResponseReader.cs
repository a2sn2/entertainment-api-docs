using System.Net;
using System.Text.Json;

namespace FoundationKit.Blazor.Api;

public static class ApiResponseReader
{
    public static async Task<ApiError> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var body = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                var code = ReadString(root, "code") ?? ReadString(root, "title");
                var message = ReadString(root, "detail")
                    ?? ReadString(root, "error")
                    ?? ReadString(root, "title");
                var correlationId = ReadString(root, "correlationId")
                    ?? ReadCorrelationHeader(response);

                if (!string.IsNullOrWhiteSpace(message))
                {
                    return new ApiError(
                        code ?? $"Http.{(int)response.StatusCode}",
                        message,
                        response.StatusCode,
                        correlationId);
                }
            }
            catch (JsonException)
            {
                return new ApiError(
                    $"Http.{(int)response.StatusCode}",
                    body,
                    response.StatusCode,
                    ReadCorrelationHeader(response));
            }
        }

        return new ApiError(
            $"Http.{(int)response.StatusCode}",
            $"The API returned {(int)response.StatusCode} {response.ReasonPhrase}.",
            response.StatusCode,
            ReadCorrelationHeader(response));
    }

    public static ApiError NetworkFailure(HttpRequestException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ApiError(
            "Network.Unavailable",
            $"The API could not be reached: {exception.Message}");
    }

    public static ApiError Timeout() =>
        new("Network.Timeout", "The API request timed out before a response was received.");

    public static ApiError InvalidPayload(HttpStatusCode? statusCode) =>
        new(
            "Response.InvalidJson",
            "The API returned a successful response with an invalid JSON payload.",
            statusCode);

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string? ReadCorrelationHeader(HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-Correlation-ID", out var values)
            ? values.FirstOrDefault()
            : null;
}
