using System.Net.Http.Json;
using System.Security.Claims;
using FoundationKit.Blazor.Api;
using Madar.Contracts.Cases;
using Madar.Contracts.Security;
using Microsoft.AspNetCore.Components.Authorization;

namespace Madar.Client.Services;

public sealed class MadarApiClient(HttpClient httpClient)
    : ApiClientBase(httpClient)
{
    private string? _antiforgeryToken;

    public Task<ApiResult<CurrentUserResponse>> GetCurrentUserAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<CurrentUserResponse>(
            new HttpRequestMessage(
                HttpMethod.Get,
                MadarSecurityRoutes.CurrentUser),
            cancellationToken);

    public async Task<ApiResult<CurrentUserResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SendProtectedAsync<CurrentUserResponse>(
            HttpMethod.Post,
            MadarSecurityRoutes.Login,
            request,
            cancellationToken);
        ResetAntiforgeryAfterIdentityChange(result.IsSuccess);
        return result;
    }

    public async Task<ApiResult<ApiMessageResponse>> LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await SendProtectedAsync<ApiMessageResponse>(
            HttpMethod.Post,
            MadarSecurityRoutes.Logout,
            new { },
            cancellationToken);
        ResetAntiforgeryAfterIdentityChange(result.IsSuccess);
        return result;
    }

    public Task<ApiResult<CaseDto[]>> ListCasesAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<CaseDto[]>(
            new HttpRequestMessage(HttpMethod.Get, CaseRoutes.Root),
            cancellationToken);

    public Task<ApiResult<CaseDto>> GetCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        SendAsync<CaseDto>(
            new HttpRequestMessage(HttpMethod.Get, CaseRoutes.ById(caseId)),
            cancellationToken);

    public Task<ApiResult<CaseDto>> CreateCaseAsync(
        CreateCaseRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<CaseDto>(
            HttpMethod.Post,
            CaseRoutes.Root,
            request,
            cancellationToken);

    public Task<ApiResult<CaseDto>> AssignCaseAsync(
        Guid caseId,
        AssignCaseRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<CaseDto>(
            HttpMethod.Post,
            CaseRoutes.Assign(caseId),
            request,
            cancellationToken);

    public Task<ApiResult<CaseDto>> TransitionCaseAsync(
        Guid caseId,
        TransitionCaseRequest request,
        CancellationToken cancellationToken = default) =>
        SendProtectedAsync<CaseDto>(
            HttpMethod.Post,
            CaseRoutes.Transition(caseId),
            request,
            cancellationToken);

    public Task<ApiResult<CaseTimelineEntryDto[]>> GetTimelineAsync(
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        SendAsync<CaseTimelineEntryDto[]>(
            new HttpRequestMessage(
                HttpMethod.Get,
                CaseTimelineRoutes.ForCase(caseId)),
            cancellationToken);

    public Task<ApiResult<OperatorOptionDto[]>> GetOperatorsAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync<OperatorOptionDto[]>(
            new HttpRequestMessage(
                HttpMethod.Get,
                MadarSecurityRoutes.Operators),
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
        request.Headers.TryAddWithoutValidation(
            "X-CSRF-TOKEN",
            tokenResult.Value);

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
            new HttpRequestMessage(
                HttpMethod.Get,
                MadarSecurityRoutes.Antiforgery),
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

        _antiforgeryToken = result.Value.Token;
        return ApiResult<string>.Success(_antiforgeryToken);
    }

    private void ResetAntiforgeryAfterIdentityChange(bool identityChanged)
    {
        if (identityChanged)
            _antiforgeryToken = null;
    }
}

public sealed class MadarAuthenticationStateProvider(
    MadarApiClient apiClient) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous =
        new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var result = await apiClient.GetCurrentUserAsync();
        if (result.IsFailure
            || result.Value is null
            || !result.Value.IsAuthenticated
            || result.Value.UserId is null)
        {
            return new AuthenticationState(Anonymous);
        }

        var user = result.Value;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.Value.ToString("D")),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? "مستخدم"),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(
            user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new AuthenticationState(
            new ClaimsPrincipal(
                new ClaimsIdentity(claims, "MadarCookie")));
    }

    public void Refresh() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
