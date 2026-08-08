using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Authorization;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseTimelineQueryService
{
    Task<IReadOnlyList<CaseTimelineEntryDto>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);
}

public interface ICaseTimelineService
{
    Task<Result<IReadOnlyList<CaseTimelineEntryDto>>> GetAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);
}

public sealed class CaseTimelineService(
    ICurrentUser currentUser,
    IAuthorizationEvaluator authorization,
    IRepository<Case, Guid> caseRepository,
    ICaseTimelineQueryService queryService) : ICaseTimelineService
{
    public async Task<Result<IReadOnlyList<CaseTimelineEntryDto>>> GetAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<IReadOnlyList<CaseTimelineEntryDto>>.Failure(
                CaseApplicationErrors.AuthenticationRequired);
        }

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null
            || (item.CreatedByUserId != currentUser.UserId.Value
                && item.AssignedToUserId != currentUser.UserId.Value
                && !authorization.HasPermission(MadarPermissions.ReadAllCases)))
        {
            return Result<IReadOnlyList<CaseTimelineEntryDto>>.Failure(
                CaseApplicationErrors.CaseNotFound);
        }

        return Result<IReadOnlyList<CaseTimelineEntryDto>>.Success(
            await queryService.ListAsync(caseId, cancellationToken));
    }
}
