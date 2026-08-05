using System.Net;
using System.Net.Http.Json;
using EntertainmentDocs.Contracts.Documents;

namespace EntertainmentDocs.Client.Services;

public sealed class DocumentationApiClient(HttpClient httpClient)
{
    public async Task<ClientApiResult<IReadOnlyList<DocumentSummaryResponse>>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.GetAsync("api/v1/documents", ct);
            if (!response.IsSuccessStatusCode)
                return ClientApiResult<IReadOnlyList<DocumentSummaryResponse>>.Failure(
                    $"The API returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                    response.StatusCode);

            var documents = await response.Content.ReadFromJsonAsync<DocumentSummaryResponse[]>(cancellationToken: ct) ?? [];
            return ClientApiResult<IReadOnlyList<DocumentSummaryResponse>>.Success(documents, response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ClientApiResult<IReadOnlyList<DocumentSummaryResponse>>.Failure(
                $"The documentation API could not be reached: {exception.Message}");
        }
    }

    public async Task<ClientApiResult<DocumentDetailsResponse>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.GetAsync($"api/v1/documents/{Uri.EscapeDataString(slug)}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return ClientApiResult<DocumentDetailsResponse>.Failure("The requested document was not found.", response.StatusCode);
            if (!response.IsSuccessStatusCode)
                return ClientApiResult<DocumentDetailsResponse>.Failure(
                    $"The API returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                    response.StatusCode);

            var document = await response.Content.ReadFromJsonAsync<DocumentDetailsResponse>(cancellationToken: ct);
            return document is null
                ? ClientApiResult<DocumentDetailsResponse>.Failure("The API returned an empty document response.", response.StatusCode)
                : ClientApiResult<DocumentDetailsResponse>.Success(document, response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ClientApiResult<DocumentDetailsResponse>.Failure(
                $"The documentation API could not be reached: {exception.Message}");
        }
    }
}

public sealed record ClientApiResult<T>(bool Succeeded, T? Value, string? Error, HttpStatusCode? StatusCode)
{
    public static ClientApiResult<T> Success(T value, HttpStatusCode? statusCode = null) =>
        new(true, value, null, statusCode);

    public static ClientApiResult<T> Failure(string error, HttpStatusCode? statusCode = null) =>
        new(false, default, error, statusCode);
}
