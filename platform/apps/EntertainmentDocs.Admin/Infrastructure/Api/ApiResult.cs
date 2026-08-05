using System.Net;
using System.Net.Http.Json;
using EntertainmentDocs.Contracts.Common;

namespace EntertainmentDocs.Admin.Infrastructure.Api;

public sealed record ApiResult(bool Succeeded, string? Error, HttpStatusCode? StatusCode)
{
    public static ApiResult Success(HttpStatusCode? statusCode = null) => new(true, null, statusCode);

    public static ApiResult Failure(string error, HttpStatusCode? statusCode = null) => new(false, error, statusCode);
}

public sealed record ApiResult<T>(bool Succeeded, T? Value, string? Error, HttpStatusCode? StatusCode)
{
    public static ApiResult<T> Success(T value, HttpStatusCode? statusCode = null) => new(true, value, null, statusCode);

    public static ApiResult<T> Failure(string error, HttpStatusCode? statusCode = null) => new(false, default, error, statusCode);
}

public static class ApiResponseReader
{
    public static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken: ct);
            if (!string.IsNullOrWhiteSpace(error?.Error))
                return error.Error;
        }
        catch (NotSupportedException)
        {
        }
        catch (System.Text.Json.JsonException)
        {
        }

        var text = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(text)
            ? $"The API returned {(int)response.StatusCode} {response.ReasonPhrase}."
            : text;
    }
}
