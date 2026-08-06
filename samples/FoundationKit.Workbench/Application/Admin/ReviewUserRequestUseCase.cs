using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Workbench.Contracts.Admin;
using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Application.Admin;

public sealed class ReviewUserRequestUseCase(
    IRepository<BuildBrief, Guid> requestRepository,
    IRepository<AdminReview, Guid> reviewRepository,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<Result<AdminReview>> ExecuteAsync(
        Guid requestId,
        AdminReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.TryParse<AdminReviewDecision>(
                request.Decision,
                ignoreCase: true,
                out var decision))
        {
            return Result<AdminReview>.Failure(AdminReviewErrors.InvalidDecision);
        }

        var brief = await requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (brief is null)
            return Result<AdminReview>.Failure(AdminReviewErrors.RequestNotFound(requestId));

        var reviewResult = AdminReview.Create(
            requestId,
            decision,
            request.ReviewedBy,
            request.Notes,
            clock.UtcNow);

        if (reviewResult.IsFailure)
            return reviewResult;

        var transitionResult = brief.ApplyReview(decision, reviewResult.Value.ReviewedUtc);
        if (transitionResult.IsFailure)
            return Result<AdminReview>.Failure(transitionResult.Error);

        await reviewRepository.AddAsync(reviewResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return reviewResult;
    }
}
