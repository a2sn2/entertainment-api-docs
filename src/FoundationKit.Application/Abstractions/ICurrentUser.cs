namespace FoundationKit.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    bool IsInRole(string role);
}
