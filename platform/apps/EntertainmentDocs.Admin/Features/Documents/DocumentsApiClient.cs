using System.Net.Http.Json;
using EntertainmentDocs.Admin.Infrastructure.Api;
using EntertainmentDocs.Contracts.Documents;

namespace EntertainmentDocs.Admin.Features.Documents;

public sealed class DocumentsApiClient(HttpClient httpClient, AuthenticatedRequestFactory requestFactory)
{
    public async Task<ApiResult<CreatedDocumentResponse>> CreateAsync(CreateDocumentRequest model, CancellationToken ct = default)
    {
        try
        {
            using var request = await requestFactory.CreateAsync(
                HttpMethod.Post,
                "api/v1/admin/documents",
                JsonContent.Create(model));
            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return ApiResult<CreatedDocumentResponse>.Failure(await ApiResponseReader.ReadErrorAsync(response, ct), response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<CreatedDocumentResponse>(cancellationToken: ct);
            return created is null
                ? ApiResult<CreatedDocumentResponse>.Failure("The API did not return the created document identifier.", response.StatusCode)
                : ApiResult<CreatedDocumentResponse>.Success(created, response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ApiResult<CreatedDocumentResponse>.Failure($"The API could not be reached: {exception.Message}");
        }
    }

    public async Task<ApiResult<CreatedDocumentVersionResponse>> AddVersionAsync(
        Guid documentId,
        AddDocumentVersionRequest model,
        CancellationToken ct = default)
    {
        try
        {
            using var request = await requestFactory.CreateAsync(
                HttpMethod.Post,
                $"api/v1/admin/documents/{documentId}/versions",
                JsonContent.Create(model));
            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return ApiResult<CreatedDocumentVersionResponse>.Failure(await ApiResponseReader.ReadErrorAsync(response, ct), response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<CreatedDocumentVersionResponse>(cancellationToken: ct);
            return created is null
                ? ApiResult<CreatedDocumentVersionResponse>.Failure("The API did not return the created version identifier.", response.StatusCode)
                : ApiResult<CreatedDocumentVersionResponse>.Success(created, response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ApiResult<CreatedDocumentVersionResponse>.Failure($"The API could not be reached: {exception.Message}");
        }
    }

    public Task<ApiResult> SubmitForReviewAsync(Guid documentId, CancellationToken ct = default) =>
        PostWithoutBodyAsync($"api/v1/admin/documents/{documentId}/submit-review", ct);

    public Task<ApiResult> PublishAsync(Guid documentId, CancellationToken ct = default) =>
        PostWithoutBodyAsync($"api/v1/admin/documents/{documentId}/publish", ct);

    private async Task<ApiResult> PostWithoutBodyAsync(string requestUri, CancellationToken ct)
    {
        try
        {
            using var request = await requestFactory.CreateAsync(HttpMethod.Post, requestUri);
            using var response = await httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode
                ? ApiResult.Success(response.StatusCode)
                : ApiResult.Failure(await ApiResponseReader.ReadErrorAsync(response, ct), response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ApiResult.Failure($"The API could not be reached: {exception.Message}");
        }
    }
}
