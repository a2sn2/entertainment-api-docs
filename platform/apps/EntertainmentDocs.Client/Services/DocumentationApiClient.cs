using EntertainmentDocs.Contracts.Documents;
using FoundationKit.Blazor.Api;

namespace EntertainmentDocs.Client.Services;

public sealed class DocumentationApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public async Task<ApiResult<IReadOnlyList<DocumentSummaryResponse>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/documents");
        var result = await SendAsync<DocumentSummaryResponse[]>(request, cancellationToken);
        return result.IsSuccess
            ? ApiResult<IReadOnlyList<DocumentSummaryResponse>>.Success(result.Value ?? [], result.StatusCode)
            : ApiResult<IReadOnlyList<DocumentSummaryResponse>>.Failure(result.Error!);
    }

    public async Task<ApiResult<DocumentDetailsResponse>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/v1/documents/{Uri.EscapeDataString(slug)}");
        return await SendAsync<DocumentDetailsResponse>(request, cancellationToken);
    }
}
