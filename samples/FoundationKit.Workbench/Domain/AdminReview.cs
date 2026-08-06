using FoundationKit.Application.Results;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Workbench.Domain;

public sealed class AdminReview : AggregateRoot<Guid>
{
    private AdminReview()
    {
    }

    private AdminReview(
        Guid id,
        Guid buildBriefId,
        AdminReviewDecision decision,
        string reviewedBy,
        string notes,
        DateTimeOffset reviewedUtc)
        : base(id)
    {
        BuildBriefId = buildBriefId;
        Decision = decision;
        ReviewedBy = reviewedBy;
        Notes = notes;
        ReviewedUtc = reviewedUtc;
    }

    public Guid BuildBriefId { get; private set; }
    public AdminReviewDecision Decision { get; private set; }
    public string ReviewedBy { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public DateTimeOffset ReviewedUtc { get; private set; }

    public static Result<AdminReview> Create(
        Guid buildBriefId,
        AdminReviewDecision decision,
        string? reviewedBy,
        string? notes,
        DateTimeOffset reviewedUtc)
    {
        var normalizedReviewer = reviewedBy?.Trim() ?? string.Empty;
        var normalizedNotes = notes?.Trim() ?? string.Empty;

        if (buildBriefId == Guid.Empty)
            return Result<AdminReview>.Failure(AdminReviewErrors.InvalidRequestId);

        if (normalizedReviewer.Length is < 2 or > 120)
            return Result<AdminReview>.Failure(AdminReviewErrors.InvalidReviewer);

        if (normalizedNotes.Length > 1200)
            return Result<AdminReview>.Failure(AdminReviewErrors.InvalidNotes);

        return Result<AdminReview>.Success(new AdminReview(
            Guid.NewGuid(),
            buildBriefId,
            decision,
            normalizedReviewer,
            normalizedNotes,
            reviewedUtc));
    }
}
