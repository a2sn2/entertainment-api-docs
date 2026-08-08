using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;

namespace Madar.Application.Cases;

public interface ICaseSlaManager
{
    Task<Result<CaseSlaEvaluationResponse>> EvaluateAsync(
        EvaluateCaseSlaRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class CaseSlaManager(
    ICurrentUser currentUser,
    IAuthorizationEvaluator authorization,
    ICaseSlaQueryService queryService,
    IRepository<Case, Guid> caseRepository,
    IUnitOfWork unitOfWork,
    IAuditRecorder auditRecorder,
    IClock clock) : ICaseSlaManager
{
    private const int MaximumBatchSize = 100;

    public async Task<Result<CaseSlaEvaluationResponse>> EvaluateAsync(
        EvaluateCaseSlaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result<CaseSlaEvaluationResponse>.Failure(
                CaseApplicationErrors.AuthenticationRequired);
        }

        if (!authorization.HasPermission(MadarPermissions.EvaluateSla))
        {
            return Result<CaseSlaEvaluationResponse>.Failure(
                CaseSlaApplicationErrors.EvaluationForbidden);
        }

        if (request.Limit is < 1 or > MaximumBatchSize)
        {
            return Result<CaseSlaEvaluationResponse>.Failure(
                CaseSlaApplicationErrors.InvalidEvaluationLimit);
        }

        var evaluatedUtc = clock.UtcNow;
        var dueIds = await queryService.ListDueCaseIdsAsync(
            evaluatedUtc,
            request.Limit + 1,
            cancellationToken);
        var hasMore = dueIds.Count > request.Limit;
        var selectedIds = dueIds.Take(request.Limit).ToArray();

        var evaluatedCount = 0;
        var breachedCount = 0;

        foreach (var caseId in selectedIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = await caseRepository.GetByIdAsync(caseId, cancellationToken);
            if (item is null)
                continue;

            evaluatedCount++;
            if (!item.EvaluateSla(evaluatedUtc))
                continue;

            breachedCount++;
            await auditRecorder.RecordAsync(
                new AuditRequest(
                    "madar.case.sla-breached",
                    nameof(Case),
                    item.Id.ToString("D"),
                    Attributes: new Dictionary<string, string>
                    {
                        ["priority"] = item.Priority,
                        ["slaTargetUtc"] = item.SlaTargetUtc!.Value.ToString(
                            "O",
                            System.Globalization.CultureInfo.InvariantCulture),
                        ["escalatedUtc"] = item.EscalatedUtc!.Value.ToString(
                            "O",
                            System.Globalization.CultureInfo.InvariantCulture)
                    }),
                cancellationToken);
        }

        if (breachedCount > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CaseSlaEvaluationResponse>.Success(
            new CaseSlaEvaluationResponse(
                evaluatedUtc,
                evaluatedCount,
                breachedCount,
                hasMore));
    }
}

public static class CaseSlaApplicationErrors
{
    public static readonly Error EvaluationForbidden = Error.Forbidden(
        "Madar.SlaEvaluationForbidden",
        "لا تملك صلاحية تقييم مهلات SLA للحالات.");

    public static readonly Error InvalidEvaluationLimit = Error.Validation(
        "Madar.InvalidSlaEvaluationLimit",
        "حجم دفعة تقييم SLA يجب أن يكون بين 1 و100 حالة.");
}
