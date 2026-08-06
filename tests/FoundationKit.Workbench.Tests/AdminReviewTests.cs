using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Tests;

public sealed class AdminReviewTests
{
    [Fact]
    public void Valid_admin_review_is_created_for_user_request()
    {
        var reviewedUtc = new DateTimeOffset(2026, 8, 6, 13, 0, 0, TimeSpan.Zero);

        var result = AdminReview.Create(
            Guid.NewGuid(),
            AdminReviewDecision.Approve,
            "Operations Admin",
            "Reviewed from the admin portal.",
            reviewedUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(AdminReviewDecision.Approve, result.Value.Decision);
        Assert.Equal(reviewedUtc, result.Value.ReviewedUtc);
    }

    [Fact]
    public void Empty_reviewer_is_rejected()
    {
        var result = AdminReview.Create(
            Guid.NewGuid(),
            AdminReviewDecision.Reject,
            " ",
            null,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("AdminReview.InvalidReviewer", result.Error.Code);
    }
}
