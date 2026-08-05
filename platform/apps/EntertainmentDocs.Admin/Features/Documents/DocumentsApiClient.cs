using System.Net.Http.Json;
using EntertainmentDocs.Admin.Infrastructure.Api;
using EntertainmentDocs.Contracts.Documents;
using FoundationKit.Blazor.Api;

namespace EntertainmentDocs.Admin.Features.Documents;

public sealed class DocumentsApiClient(
    HttpClient httpClient,
    AuthenticatedRequestFactory requestFactory)
    : ApiClientBase(httpClient)
{
    public async Task<ApiResult<CreatedDocumentResponse>> CreateAsync(
        CreateDocumentRequest model,
        CancellationToken cancellationToken = default)
    {
        using var request = await requestFactory.CreateAsync(
            HttpMethod.Post,
            "api/v1/admin/documents",
            JsonContent.Create(model));
        return await SendAsync<CreatedDocumentResponse>(request, cancellationToken);
    }

    public async Task<ApiResult<CreatedDocumentVersionResponse>> AddVersionAsync(
        Guid documentId,
        AddDocumentVersionRequest model,
        CancellationToken cancellationToken = default)
    {
        using var request = await requestFactory.CreateAsync(
            HttpMethod.Post,
            $"api/v1/admin/documents/{documentId}/versions",
            JsonContent.Create(model));
        return await SendAsync<CreatedDocumentVersionResponse>(request, cancellationToken);
    }

    public Task<ApiResult> SubmitForReviewAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        PostWithoutBodyAsync($"api/v1/admin/documents/{documentId}/submit-review", cancellationToken);

    public Task<ApiResult> PublishAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        PostWithoutBodyAsync($"api/v1/admin/documents/{documentId}/publish", cancellationToken);

    private async Task<ApiResult> PostWithoutBodyAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        using var request = await requestFactory.CreateAsync(HttpMethod.Post, requestUri);
        return await SendAsync(request, cancellationToken);
    }
}
