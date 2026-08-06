using Athar.Contracts;
using FoundationKit.Application.Pagination;
using FoundationKit.Application.Results;

namespace Athar.Application;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    bool IsInRole(string role);
}

public interface IInitiativeQueryService
{
    Task<InitiativeDetailsDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InitiativeDetailsDto?> FindByClientRequestIdAsync(
        Guid ownerUserId,
        Guid clientRequestId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<InitiativeSummaryDto>> GetMineAsync(
        Guid ownerUserId,
        InitiativeSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<InitiativeSummaryDto>> GetAdminQueueAsync(
        InitiativeSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminDashboardResponse> GetDashboardAsync(
        CancellationToken cancellationToken = default);
}

public interface IAuditWriter
{
    Task WriteAsync(
        Guid? userId,
        string action,
        string entityType,
        Guid entityId,
        string details,
        CancellationToken cancellationToken = default);
}

public interface IInitiativeManager
{
    Task<Result<InitiativeDetailsDto>> CreateAsync(
        CreateInitiativeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<InitiativeDetailsDto>> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<InitiativeSummaryDto>>> GetMineAsync(
        InitiativeSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<InitiativeSummaryDto>>> GetAdminQueueAsync(
        InitiativeSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AdminDashboardResponse>> GetDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<Result<InitiativeDetailsDto>> ReviewAsync(
        Guid id,
        ReviewInitiativeRequest request,
        CancellationToken cancellationToken = default);
}
