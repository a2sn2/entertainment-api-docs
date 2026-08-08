namespace Madar.Application.Security;

public interface IUserDirectory
{
    Task<bool> IsAssignableOperatorAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
