using System.Net.Http.Json;
using EntertainmentDocs.Admin.Infrastructure.Api;
using EntertainmentDocs.Admin.Infrastructure.Authentication;
using EntertainmentDocs.Contracts.Authentication;

namespace EntertainmentDocs.Admin.Features.Authentication;

public sealed class AuthenticationApiClient(
    HttpClient httpClient,
    JwtAuthenticationStateProvider authenticationStateProvider)
{
    public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync("api/v1/auth/login", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var message = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "The email or password is incorrect, or the account is inactive."
                    : await ApiResponseReader.ReadErrorAsync(response, ct);
                return ApiResult<LoginResponse>.Failure(message, response.StatusCode);
            }

            var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
            if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
                return ApiResult<LoginResponse>.Failure("The login response did not contain an access token.", response.StatusCode);

            await authenticationStateProvider.SetAuthenticatedAsync(payload.AccessToken);
            return ApiResult<LoginResponse>.Success(payload, response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ApiResult<LoginResponse>.Failure($"The API could not be reached: {exception.Message}");
        }
    }

    public Task LogoutAsync() => authenticationStateProvider.SignOutAsync();
}
