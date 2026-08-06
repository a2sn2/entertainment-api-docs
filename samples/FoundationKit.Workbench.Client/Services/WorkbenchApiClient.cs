using System.Net.Http.Json;
using FoundationKit.Blazor.Api;
using FoundationKit.Workbench.Contracts;

namespace FoundationKit.Workbench.Client.Services;

public sealed class WorkbenchApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<RuntimeResponse>> GetRuntimeAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<RuntimeResponse>(
            new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Runtime),
            cancellationToken);

    public async Task<ApiResult<CatalogResponse>> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var apiResult = await SendAsync<CatalogResponse>(
            new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Catalog),
            cancellationToken);

        if (apiResult.IsSuccess)
            return apiResult;

        return await SendAsync<CatalogResponse>(
            new HttpRequestMessage(HttpMethod.Get, "catalog/foundationkit.catalog.json"),
            cancellationToken);
    }

    public Task<ApiResult<HealthResponse>> GetHealthAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<HealthResponse>(
            new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Health),
            cancellationToken);

    public Task<ApiResult<BuildBriefResponse>> CreateBuildBriefAsync(
        BuildBriefRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendAsync<BuildBriefResponse>(
            new HttpRequestMessage(HttpMethod.Post, ApiRoutes.BuildBriefs)
            {
                Content = JsonContent.Create(request)
            },
            cancellationToken);
    }

    public Task<ApiResult<BuildBriefResponse>> GetBuildBriefAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        SendAsync<BuildBriefResponse>(
            new HttpRequestMessage(HttpMethod.Get, ApiRoutes.BuildBrief(id)),
            cancellationToken);
}
