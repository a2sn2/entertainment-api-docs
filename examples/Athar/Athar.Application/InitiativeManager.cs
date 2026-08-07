using Athar.Contracts;
using Athar.Domain;
using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Pagination;
using FoundationKit.Application.Results;
using FoundationKit.Approvals;
using FoundationKit.Authorization;

namespace Athar.Application;

public sealed class InitiativeManager(
    ICurrentUser currentUser,
    IAuthorizationEvaluator authorization,
    IInitiativeQueryService queryService,
    IRepository<Initiative, Guid> initiativeRepository,
    IRepository<InitiativeReview, Guid> reviewRepository,
    IUnitOfWork unitOfWork,
    IAuditWriter auditWriter,
    IClock clock) : IInitiativeManager
{
    public async Task<Result<InitiativeDetailsDto>> CreateAsync(
        CreateInitiativeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<InitiativeDetailsDto>.Failure(
                InitiativeErrors.AuthenticationRequired);

        var existing = await queryService.FindByClientRequestIdAsync(
            currentUser.UserId.Value,
            request.ClientRequestId,
            cancellationToken);

        if (existing is not null)
            return Result<InitiativeDetailsDto>.Success(existing);

        var creation = Initiative.Create(
            request.ClientRequestId,
            currentUser.UserId.Value,
            request.Title,
            request.Summary,
            request.Category,
            request.City,
            request.RequestedBudget,
            request.TargetBeneficiaries,
            clock.UtcNow);

        if (creation.IsFailure)
            return Result<InitiativeDetailsDto>.Failure(creation.Error);

        await initiativeRepository.AddAsync(creation.Value, cancellationToken);
        await auditWriter.WriteAsync(
            currentUser.UserId,
            "initiative.created",
            nameof(Initiative),
            creation.Value.Id,
            $"تم إنشاء مبادرة بعنوان: {creation.Value.Title}",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await queryService.GetByIdAsync(
            creation.Value.Id,
            cancellationToken);

        return response is null
            ? Result<InitiativeDetailsDto>.Failure(InitiativeErrors.InitiativeNotFound)
            : Result<InitiativeDetailsDto>.Success(response);
    }

    public async Task<Result<InitiativeDetailsDto>> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<InitiativeDetailsDto>.Failure(
                InitiativeErrors.AuthenticationRequired);

        var response = await queryService.GetByIdAsync(id, cancellationToken);
        if (response is null)
            return Result<InitiativeDetailsDto>.Failure(
                InitiativeErrors.InitiativeNotFound);

        if (!authorization.HasPermission(AtharPermissions.ReadAllInitiatives))
        {
            var entity = await initiativeRepository.GetByIdAsync(id, cancellationToken);
            if (entity is null
                || !authorization.CanAccessOwnedResource(
                    entity.OwnerUserId,
                    AtharPermissions.ReadAllInitiatives))
            {
                return Result<InitiativeDetailsDto>.Failure(
                    InitiativeErrors.InitiativeNotFound);
            }
        }

        return Result<InitiativeDetailsDto>.Success(response);
    }

    public async Task<Result<PagedResult<InitiativeSummaryDto>>> GetMineAsync(
        InitiativeSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<PagedResult<InitiativeSummaryDto>>.Failure(
                InitiativeErrors.AuthenticationRequired);

        var result = await queryService.GetMineAsync(
            currentUser.UserId.Value,
            request,
            cancellationToken);

        return Result<PagedResult<InitiativeSummaryDto>>.Success(result);
    }

    public async Task<Result<PagedResult<InitiativeSummaryDto>>> GetAdminQueueAsync(
        InitiativeSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!authorization.HasPermission(AtharPermissions.ReadAllInitiatives))
            return Result<PagedResult<InitiativeSummaryDto>>.Failure(
                InitiativeErrors.AdministratorRequired);

        var result = await queryService.GetAdminQueueAsync(
            request,
            cancellationToken);

        return Result<PagedResult<InitiativeSummaryDto>>.Success(result);
    }

    public async Task<Result<AdminDashboardResponse>> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        if (!authorization.HasPermission(AtharPermissions.ReadDashboard))
            return Result<AdminDashboardResponse>.Failure(
                InitiativeErrors.AdministratorRequired);

        return Result<AdminDashboardResponse>.Success(
            await queryService.GetDashboardAsync(cancellationToken));
    }

    public async Task<Result<InitiativeDetailsDto>> ReviewAsync(
        Guid id,
        ReviewInitiativeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (currentUser.UserId is null
            || !ApprovalPolicy.HasDecisionPermission(
                authorization,
                AtharPermissions.ReviewInitiatives))
        {
            return Result<InitiativeDetailsDto>.Failure(
                InitiativeErrors.AdministratorRequired);
        }

        var initiative = await initiativeRepository.GetByIdAsync(id, cancellationToken);
        if (initiative is null)
            return Result<InitiativeDetailsDto>.Failure(
                InitiativeErrors.InitiativeNotFound);

        var eligibility = ApprovalPolicy.Evaluate(
            authorization,
            AtharPermissions.ReviewInitiatives,
            initiative.OwnerUserId.ToString("D"),
            currentUser.UserId.Value.ToString("D"));

        if (eligibility == ApprovalEligibility.PermissionDenied)
        {
            return Result<InitiativeDetailsDto>.Failure(
                InitiativeErrors.AdministratorRequired);
        }

        if (eligibility == ApprovalEligibility.MakerCheckerViolation)
        {
            return Result<InitiativeDetailsDto>.Failure(
                InitiativeErrors.SelfReviewNotAllowed);
        }

        if (!ApprovalDecisions.TryParse(request.Decision, out var decision))
        {
            return Result<InitiativeDetailsDto>.Failure(
                InitiativeErrors.InvalidDecision);
        }

        var normalizedDecision = ApprovalDecisions.ToTrigger(decision);
        if (!ApprovalDecisions.TryResolve(
                InitiativeWorkflow.Definition,
                initiative.Status,
                normalizedDecision,
                out var approval))
        {
            return Result<InitiativeDetailsDto>.Failure(
                InitiativeErrors.AlreadyReviewed);
        }

        var reviewResult = initiative.Review(
            approval.DecisionToken,
            currentUser.UserId.Value,
            request.Notes,
            clock.UtcNow);

        if (reviewResult.IsFailure)
            return Result<InitiativeDetailsDto>.Failure(reviewResult.Error);

        var review = InitiativeReview.Create(
            initiative.Id,
            currentUser.UserId.Value,
            approval.DecisionToken,
            request.Notes,
            clock.UtcNow);

        await reviewRepository.AddAsync(review, cancellationToken);
        await auditWriter.WriteAsync(
            currentUser.UserId,
            "initiative.reviewed",
            nameof(Initiative),
            initiative.Id,
            $"تم اتخاذ القرار: {request.Decision}",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await queryService.GetByIdAsync(id, cancellationToken);

        return response is null
            ? Result<InitiativeDetailsDto>.Failure(InitiativeErrors.InitiativeNotFound)
            : Result<InitiativeDetailsDto>.Success(response);
    }
}
