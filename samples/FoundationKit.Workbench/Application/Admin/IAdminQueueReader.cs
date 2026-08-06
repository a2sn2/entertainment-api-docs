using FoundationKit.Workbench.Contracts.Admin;

namespace FoundationKit.Workbench.Application.Admin;

public interface IAdminQueueReader
{
    Task<IReadOnlyList<AdminQueueItemResponse>> ReadAsync(
        string? status,
        CancellationToken cancellationToken = default);
}
