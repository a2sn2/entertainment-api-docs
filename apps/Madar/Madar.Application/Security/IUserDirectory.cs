namespace Madar.Application.Security;

public interface IUserDirectory
{
    Task<bool> ExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
