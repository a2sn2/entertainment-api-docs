using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseCommentManager
{
    Task<Result<IReadOnlyList<CaseCommentDto>>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<Result<CaseCommentDto>> AddAsync(
        Guid caseId,
        AddCaseCommentRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class CaseCommentManager(
    ICurrentUser currentUser,
    IAuthorizationEvaluator authorization,
    IRepository<Case, Guid> caseRepository,
    ICaseCommentStore commentStore,
    ICaseCommentQueryService queryService,
    IUnitOfWork unitOfWork,
    IAuditRecorder auditRecorder,
    IClock clock) : ICaseCommentManager
{
    public async Task<Result<IReadOnlyList<CaseCommentDto>>> ListAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var access = await AuthorizeCaseAsync(caseId, cancellationToken);
        if (access.IsFailure)
        {
            return Result<IReadOnlyList<CaseCommentDto>>.Failure(access.Error);
        }

        var comments = await queryService.ListForCaseAsync(
            caseId,
            cancellationToken);
        return Result<IReadOnlyList<CaseCommentDto>>.Success(comments);
    }

    public async Task<Result<CaseCommentDto>> AddAsync(
        Guid caseId,
        AddCaseCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await AuthorizeCaseAsync(caseId, cancellationToken);
        if (access.IsFailure)
            return Result<CaseCommentDto>.Failure(access.Error);

        var userId = currentUser.UserId!.Value;
        var creation = CaseComment.Create(
            caseId,
            userId,
            request.Body,
            clock.UtcNow);
        if (creation.IsFailure)
            return Result<CaseCommentDto>.Failure(creation.Error);

        await commentStore.AddAsync(creation.Value, cancellationToken);
        await auditRecorder.RecordAsync(
            new AuditRequest(
                "madar.case.comment-added",
                nameof(Case),
                caseId.ToString("D"),
                Attributes: new Dictionary<string, string>
                {
                    ["commentId"] = creation.Value.Id.ToString("D")
                }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await queryService.GetByIdAsync(
            creation.Value.Id,
            cancellationToken);
        return response is null
            ? Result<CaseCommentDto>.Failure(CaseApplicationErrors.CaseNotFound)
            : Result<CaseCommentDto>.Success(response);
    }

    private async Task<Result> AuthorizeCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(CaseApplicationErrors.AuthenticationRequired);

        var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (item is null
            || !CaseAccessRules.CanRead(
                item,
                currentUser.UserId.Value,
                authorization))
        {
            return Result.Failure(CaseApplicationErrors.CaseNotFound);
        }

        return Result.Success();
    }
}
