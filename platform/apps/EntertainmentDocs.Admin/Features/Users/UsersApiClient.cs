using System.Net.Http.Json;
using EntertainmentDocs.Admin.Infrastructure.Api;
using EntertainmentDocs.Contracts.Users;

namespace EntertainmentDocs.Admin.Features.Users;

public sealed class UsersApiClient(HttpClient httpClient, AuthenticatedRequestFactory requestFactory)
{
    public async Task<ApiResult<IReadOnlyList<UserSummaryResponse>>> ListAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = await requestFactory.CreateAsync(HttpMethod.Get, "api/v1/admin/users");
            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return ApiResult<IReadOnlyList<UserSummaryResponse>>.Failure(await ApiResponseReader.ReadErrorAsync(response, ct), response.StatusCode);

            var users = await response.Content.ReadFromJsonAsync<UserSummaryResponse[]>(cancellationToken: ct) ?? [];
            return ApiResult<IReadOnlyList<UserSummaryResponse>>.Success(users, response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ApiResult<IReadOnlyList<UserSummaryResponse>>.Failure($"The API could not be reached: {exception.Message}");
        }
    }

    public async Task<ApiResult<CreatedUserResponse>> CreateAsync(CreateUserRequest model, CancellationToken ct = default)
    {
        try
        {
            using var request = await requestFactory.CreateAsync(
                HttpMethod.Post,
                "api/v1/admin/users",
                JsonContent.Create(model));
            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return ApiResult<CreatedUserResponse>.Failure(await ApiResponseReader.ReadErrorAsync(response, ct), response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<CreatedUserResponse>(cancellationToken: ct);
            return created is null
                ? ApiResult<CreatedUserResponse>.Failure("The API did not return the created user identifier.", response.StatusCode)
                : ApiResult<CreatedUserResponse>.Success(created, response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ApiResult<CreatedUserResponse>.Failure($"The API could not be reached: {exception.Message}");
        }
    }

    public async Task<ApiResult> ReplaceRolesAsync(Guid userId, UpdateUserRolesRequest model, CancellationToken ct = default)
    {
        try
        {
            using var request = await requestFactory.CreateAsync(
                HttpMethod.Put,
                $"api/v1/admin/users/{userId}/roles",
                JsonContent.Create(model));
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
