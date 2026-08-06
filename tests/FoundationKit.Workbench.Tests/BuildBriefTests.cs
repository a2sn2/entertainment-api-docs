using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Tests;

public sealed class BuildBriefTests
{
    [Fact]
    public void Valid_brief_is_created_submitted_with_distinct_capabilities_and_event()
    {
        var createdUtc = new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);

        var result = BuildBrief.Create(
            "Operations Portal",
            "Internal platform",
            "Operations team",
            "Create one place for approvals and operational reporting.",
            ["commands-queries", "commands-queries", "ef-repository"],
            "Auditability first",
            "No confidential information",
            createdUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.SelectedCapabilityIds.Count);
        Assert.Single(result.Value.DomainEvents);
        Assert.Equal(BuildBriefStatus.Submitted, result.Value.Status);
        Assert.Equal(createdUtc, result.Value.CreatedUtc);
        Assert.Equal(createdUtc, result.Value.UpdatedUtc);
    }

    [Fact]
    public void Admin_review_transitions_request_once_and_raises_integration_event()
    {
        var created = BuildBrief.Create(
            "Operations Portal",
            "Internal platform",
            "Operations team",
            "Create one place for approvals and operational reporting.",
            [],
            null,
            null,
            DateTimeOffset.UtcNow);

        var reviewedUtc = DateTimeOffset.UtcNow.AddMinutes(10);
        var reviewed = created.Value.ApplyReview(AdminReviewDecision.Approve, reviewedUtc);
        var secondReview = created.Value.ApplyReview(AdminReviewDecision.Reject, reviewedUtc.AddMinutes(1));

        Assert.True(reviewed.IsSuccess);
        Assert.Equal(BuildBriefStatus.Approved, created.Value.Status);
        Assert.Equal(reviewedUtc, created.Value.UpdatedUtc);
        Assert.Contains(created.Value.DomainEvents, item => item is BuildBriefReviewed);
        Assert.True(secondReview.IsFailure);
        Assert.Equal("BuildBrief.AlreadyReviewed", secondReview.Error.Code);
    }

    [Fact]
    public void Short_goal_is_rejected_as_classified_validation_error()
    {
        var result = BuildBrief.Create(
            "Portal",
            "Internal",
            "Team",
            "Too short",
            [],
            null,
            null,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("BuildBrief.InvalidGoal", result.Error.Code);
    }
}
