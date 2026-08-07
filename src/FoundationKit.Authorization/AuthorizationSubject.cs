namespace FoundationKit.Authorization;

public interface IAuthorizationSubject
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    bool IsInRole(string role);
}
