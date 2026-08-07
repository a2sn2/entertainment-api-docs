using System.Net.Http.Json;
using System.Security.Claims;
using Athar.Contracts;
using FoundationKit.Application.Pagination;
using FoundationKit.Blazor.Api;
using Microsoft.AspNetCore.Components.Authorization;

namespace Athar.Client.Services;

public sealed class AtharApiClient(HttpClient httpClient)
    : ApiClientBase(httpClient)
{
    private string? _antiforgeryToken;

    public Task<ApiResult<CurrentUserResponse>> GetCurrentUserAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<CurrentUserResponse>(
            new HttpRequestMessage(HttpMethod.Get, AtharRoutes.Me),
            cancellationToken);

    public async Task<ApiResult<CurrentUserResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendProtectedAsync<CurrentUserResponse>(
            HttpMethod.Post,
            AtharRoutes.Register,
            request,
            cancellationToken);
        ResetAntiforgeryAfterIdentityChange(result.IsSuccess);
        return result;
    }

    public async Task<ApiResult<CurrentUserResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendProtectedAsync<CurrentUserResponse>(
            HttpMethod.Post,
            AtharRoutes.Login,
            request,
            cancellationToken);
        ResetAntiforgeryAfterIdentityChange(result.IsSuccess);
        return result;
    }

    public async Task<ApiResult<CurrentUserResponse>> TwoFactorLoginAsync(
        TwoFactorLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendProtectedAsync<CurrentUserResponse>(
            HttpMethod.Post,
            AtharRoutes.LoginTwoFactor,
            request,
            cancellationToken);
        ResetAntiforgeryAfterIdentityChange(result.IsSuccess);
        return result;
    }

    public Task<ApiResult<ApiMessageResponse>> RequestEmailConfirmationAsync(
        EmailAddressRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<ApiMessageResponse>(
            HttpMethod.Post,
            AtharRoutes.RequestEmailConfirmation,
            request,
            cancellationToken);

    public Task<ApiResult<ApiMessageResponse>> ConfirmEmailAsync(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<ApiMessageResponse>(
            HttpMethod.Post,
            AtharRoutes.ConfirmEmail,
            request,
            cancellationToken);

    public Task<ApiResult<ApiMessageResponse>> ForgotPasswordAsync(
        EmailAddressRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<ApiMessageResponse>(
            HttpMethod.Post,
            AtharRoutes.ForgotPassword,
            request,
            cancellationToken);

    public Task<ApiResult<ApiMessageResponse>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<ApiMessageResponse>(
            HttpMethod.Post,
            AtharRoutes.ResetPassword,
            request,
            cancellationToken);

    public async Task<ApiResult<ApiMessageResponse>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendProtectedAsync<ApiMessageResponse>(
            HttpMethod.Post,
            AtharRoutes.ChangePassword,
            request,
            cancellationToken);
        ResetAntiforgeryAfterIdentityChange(result.IsSuccess);
        return result;
    }

    public Task<ApiResult<MfaStatusResponse>> GetMfaStatusAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<MfaStatusResponse>(
            new HttpRequestMessage(HttpMethod.Get, AtharRoutes.MfaStatus),
            cancellationToken);

    public Task<ApiResult<MfaSetupResponse>> SetupMfaAsync(
        MfaSetupRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<MfaSetupResponse>(
            HttpMethod.Post,
            AtharRoutes.MfaSetup,
            request,
            cancellationToken);

    public async Task<ApiResult<MfaEnableResponse>> EnableMfaAsync(
        MfaCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendProtectedAsync<MfaEnableResponse>(
            HttpMethod.Post,
            AtharRoutes.MfaEnable,
            request,
            cancellationToken);
        ResetAntiforgeryAfterIdentityChange(result.IsSuccess);
        return result;
    }

    public async Task<ApiResult<ApiMessageResponse>> DisableMfaAsync(
        MfaDisableRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendProtectedAsync<ApiMessageResponse>(
            HttpMethod.Post,
            AtharRoutes.MfaDisable,
            request,
            cancellationToken);
        ResetAntiforgeryAfterIdentityChange(result.IsSuccess);
        return result;
    }

    public Task<ApiResult<MfaEnableResponse>> RegenerateRecoveryCodesAsync(
        MfaRecoveryCodesRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<MfaEnableResponse>(
            HttpMethod.Post,
            AtharRoutes.MfaRecoveryCodes,
            request,
            cancellationToken);

    public async Task<ApiResult<ApiMessageResponse>> LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await SendProtectedAsync<ApiMessageResponse>(
            HttpMethod.Post,
            AtharRoutes.Logout,
            new { },
            cancellationToken);
        ResetAntiforgeryAfterIdentityChange(result.IsSuccess);
        return result;
    }

    public Task<ApiResult<InitiativeDetailsDto>> CreateInitiativeAsync(
        CreateInitiativeRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<InitiativeDetailsDto>(
            HttpMethod.Post,
            AtharRoutes.Initiatives,
            request,
            cancellationToken);

    public Task<ApiResult<PagedResult<InitiativeSummaryDto>>> GetMyInitiativesAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<InitiativeSummaryDto>>(
            new HttpRequestMessage(
                HttpMethod.Get,
                BuildQuery(AtharRoutes.MyInitiatives, page, pageSize, search, status)),
            cancellationToken);

    public Task<ApiResult<PagedResult<InitiativeSummaryDto>>> GetAdminInitiativesAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<InitiativeSummaryDto>>(
            new HttpRequestMessage(
                HttpMethod.Get,
                BuildQuery(AtharRoutes.AdminQueue, page, pageSize, search, status)),
            cancellationToken);

    public Task<ApiResult<AdminDashboardResponse>> GetAdminDashboardAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<AdminDashboardResponse>(
            new HttpRequestMessage(HttpMethod.Get, AtharRoutes.AdminDashboard),
            cancellationToken);

    public Task<ApiResult<InitiativeDetailsDto>> GetInitiativeAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        SendAsync<InitiativeDetailsDto>(
            new HttpRequestMessage(HttpMethod.Get, AtharRoutes.Initiative(id)),
            cancellationToken);

    public Task<ApiResult<InitiativeDetailsDto>> ReviewInitiativeAsync(
        Guid id,
        ReviewInitiativeRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<InitiativeDetailsDto>(
            HttpMethod.Post,
            AtharRoutes.ReviewInitiative(id),
            request,
            cancellationToken);

    private async Task<ApiResult<TResponse>> SendProtectedAsync<TResponse>(
        HttpMethod method,
        string route,
        object body,
        CancellationToken cancellationToken)
    {
        var tokenResult = await EnsureAntiforgeryTokenAsync(cancellationToken);
        if (tokenResult.IsFailure || tokenResult.Value is null)
        {
            return ApiResult<TResponse>.Failure(
                tokenResult.ErrorDetails
                ?? new ApiError(
                    "Security.TokenUnavailable",
                    "تعذر إنشاء رمز حماية الطلب.",
                    tokenResult.StatusCode));
        }

        using var request = new HttpRequestMessage(method, route)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", tokenResult.Value);

        var response = await SendAsync<TResponse>(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            _antiforgeryToken = null;
        return response;
    }

    private async Task<ApiResult<string>> EnsureAntiforgeryTokenAsync(
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_antiforgeryToken))
            return ApiResult<string>.Success(_antiforgeryToken);

        var result = await SendAsync<AntiforgeryTokenResponse>(
            new HttpRequestMessage(HttpMethod.Get, AtharRoutes.SecurityToken),
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return ApiResult<string>.Failure(
                result.ErrorDetails
                ?? new ApiError(
                    "Security.TokenUnavailable",
                    "تعذر إنشاء رمز حماية الطلب.",
                    result.StatusCode));
        }

        _antiforgeryToken = result.Value.RequestToken;
        return ApiResult<string>.Success(_antiforgeryToken);
    }

    private void ResetAntiforgeryAfterIdentityChange(bool identityChanged)
    {
        if (identityChanged)
            _antiforgeryToken = null;
    }

    private static string BuildQuery(
        string route,
        int page,
        int pageSize,
        string? search,
        string? status)
    {
        var values = new List<string>
        {
            $"page={Math.Max(1, page)}",
            $"pageSize={Math.Clamp(pageSize, 1, 200)}"
        };
        if (!string.IsNullOrWhiteSpace(search))
            values.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(status))
            values.Add($"status={Uri.EscapeDataString(status.Trim())}");
        return $"{route}?{string.Join("&", values)}";
    }
}

public sealed class AtharAuthenticationStateProvider(
    AtharApiClient apiClient) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var result = await apiClient.GetCurrentUserAsync();
        if (result.IsFailure || result.Value is null || !result.Value.IsAuthenticated)
            return new AuthenticationState(Anonymous);

        var user = result.Value;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id!.Value.ToString()),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? "مستخدم"),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "AtharCookie")));
    }

    public void Refresh() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
