namespace EntertainmentDocs.Contracts.Users;

public sealed record CreateUserRequest(
    string Email,
    string DisplayName,
    string TemporaryPassword,
    string[] Roles);

public sealed record UpdateUserRolesRequest(string[] Roles);

public sealed record CreatedUserResponse(Guid Id);

public sealed record UserSummaryResponse(
    Guid Id,
    string DisplayName,
    string? Email,
    bool IsActive,
    IReadOnlyList<string> Roles);
