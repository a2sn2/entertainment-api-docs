using System.Net.Http.Json;
using EntertainmentDocs.Admin.Infrastructure.Api;
using EntertainmentDocs.Contracts.Users;
using FoundationKit.Blazor.Api;

namespace EntertainmentDocs.Admin.Features.Users;

public sealed class UsersApiClient(
    HttpClient httpClient,
    AuthenticatedRequestFactory requestFactory)
    : ApiClientBase(httpClient)
{
    public async Task<ApiResult<IReadOnlyList<UserSummaryResponse>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var request = await requestFactory.CreateAsync(HttpMethod.Get, "api/v1/admin/users");
        var result = await SendAsync<UserSummaryResponse[]>(request, cancellationToken);
        return result.IsSuccess
            ? ApiResult<IReadOnlyList<UserSummaryResponse>>.Success(result.Value ?? [], result.StatusCode)
            : ApiResult<IReadOnlyList<UserSummaryResponse>>.Failure(result.ErrorDetails!);
    }

    public async Task<ApiResult<CreatedUserResponse>> CreateAsync(
        CreateUserRequest model,
        CancellationToken cancellationToken = default)
    {
        using var request = await requestFactory.CreateAsync(
            HttpMethod.Post,
            "api/v1/admin/users",
            JsonContent.Create(model));
        return await SendAsync<CreatedUserResponse>(request, cancellationToken);
    }

    public async Task<ApiResult> ReplaceRolesAsync(
        Guid userId,
        UpdateUserRolesRequest model,
        CancellationToken cancellationToken = default)
    {
        using var request = await requestFactory.CreateAsync(
            HttpMethod.Put,
            $"api/v1/admin/users/{userId}/roles",
            JsonContent.Create(model));
        return await SendAsync(request, cancellationToken);
    }
}
