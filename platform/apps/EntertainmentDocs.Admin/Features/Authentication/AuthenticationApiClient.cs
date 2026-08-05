using System.Net;
using System.Net.Http.Json;
using EntertainmentDocs.Admin.Infrastructure.Authentication;
using EntertainmentDocs.Contracts.Authentication;
using FoundationKit.Blazor.Api;

namespace EntertainmentDocs.Admin.Features.Authentication;

public sealed class AuthenticationApiClient(
    HttpClient httpClient,
    JwtAuthenticationStateProvider authenticationStateProvider)
    : ApiClientBase(httpClient)
{
    public async Task<ApiResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/login")
        {
            Content = JsonContent.Create(request)
        };

        var result = await SendAsync<LoginResponse>(message, cancellationToken);
        if (result.IsFailure)
        {
            return result.StatusCode == HttpStatusCode.Unauthorized
                ? ApiResult<LoginResponse>.Failure(new ApiError(
                    "Authentication.InvalidCredentials",
                    "The email or password is incorrect, or the account is inactive.",
                    result.StatusCode,
                    result.ErrorDetails?.CorrelationId))
                : result;
        }

        if (result.Value is null || string.IsNullOrWhiteSpace(result.Value.AccessToken))
        {
            return ApiResult<LoginResponse>.Failure(new ApiError(
                "Authentication.TokenMissing",
                "The login response did not contain an access token.",
                result.StatusCode));
        }

        await authenticationStateProvider.SetAuthenticatedAsync(result.Value.AccessToken);
        return result;
    }

    public Task LogoutAsync() => authenticationStateProvider.SignOutAsync();
}
