using FoundationKit.Workbench.Contracts.Admin;
using FoundationKit.Workbench.Contracts.User;
using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Endpoints;

internal static class WorkbenchContractMapper
{
    public static UserRequestResponse ToUserResponse(BuildBrief brief) => new(
        brief.Id,
        brief.ProjectName,
        brief.ProjectType,
        brief.Audience,
        brief.Goal,
        brief.SelectedCapabilityIds,
        brief.Priorities,
        brief.Notes,
        brief.Status.ToString().ToLowerInvariant(),
        brief.CreatedUtc,
        brief.UpdatedUtc,
        Application.ContactLinkBuilder.Build(brief));

    public static AdminReviewResponse ToAdminReviewResponse(AdminReview review) => new(
        review.Id,
        review.BuildBriefId,
        review.Decision is AdminReviewDecision.Approve ? "approved" : "rejected",
        review.Decision.ToString().ToLowerInvariant(),
        review.ReviewedBy,
        review.Notes,
        review.ReviewedUtc);
}
