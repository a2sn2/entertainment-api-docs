namespace EntertainmentDocs.Contracts.Authentication;

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string DisplayName,
    string? Email);

public sealed record LoginResponse(
    string AccessToken,
    AuthenticatedUserResponse User,
    IReadOnlyList<string> Roles);
