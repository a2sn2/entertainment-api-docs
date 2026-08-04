namespace EntertainmentDocs.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
