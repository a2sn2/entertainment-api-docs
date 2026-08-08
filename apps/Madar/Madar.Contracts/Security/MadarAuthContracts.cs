namespace Madar.Contracts.Security;

public sealed record LoginRequest(
    string Email,
    string Password,
    bool RememberMe);

public sealed record CurrentUserResponse(
    bool IsAuthenticated,
    Guid? UserId,
    string? Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);

public sealed record AntiforgeryTokenResponse(string Token);

public sealed record OperatorOptionDto(
    Guid UserId,
    string DisplayName,
    string Email);

public sealed record ApiMessageResponse(string Message);

public static class MadarSecurityRoutes
{
    public const string Antiforgery = "/api/security/antiforgery";
    public const string Login = "/api/auth/login";
    public const string Logout = "/api/auth/logout";
    public const string CurrentUser = "/api/auth/me";
    public const string Operators = "/api/users/operators";
}
