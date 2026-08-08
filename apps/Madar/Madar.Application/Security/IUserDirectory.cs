namespace Madar.Application.Security;

public interface IUserDirectory
{
    Task<bool> ExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    async Task<bool> IsAssignableOperatorAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await ExistsAsync(userId, cancellationToken).ConfigureAwait(false);

    Task<string?> GetNotificationDestinationAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
