using FoundationKit.Application.Results;

namespace FoundationKit.Workbench.Domain;

public static class AdminReviewErrors
{
    public static readonly Error InvalidRequestId = Error.Validation(
        "AdminReview.InvalidRequestId",
        "A valid user request identifier is required.");

    public static readonly Error InvalidReviewer = Error.Validation(
        "AdminReview.InvalidReviewer",
        "Reviewer name must contain between 2 and 120 characters.");

    public static readonly Error InvalidNotes = Error.Validation(
        "AdminReview.InvalidNotes",
        "Review notes cannot exceed 1200 characters.");

    public static readonly Error InvalidDecision = Error.Validation(
        "AdminReview.InvalidDecision",
        "Decision must be either approve or reject.");

    public static Error RequestNotFound(Guid requestId) => Error.NotFound(
        "AdminReview.RequestNotFound",
        $"User request '{requestId:D}' was not found.");
}
