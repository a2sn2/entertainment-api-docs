using FoundationKit.Application.Results;
using FoundationKit.Domain.Events;
using FoundationKit.Domain.Primitives;

namespace Athar.Domain;

public static class InitiativeStatuses
{
    public const string Submitted = "submitted";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static bool IsFinal(string status) =>
        status is Approved or Rejected;
}

public static class InitiativeDecisions
{
    public const string Approve = "approve";
    public const string Reject = "reject";

    public static bool IsValid(string? decision) =>
        decision is Approve or Reject;
}

public sealed class Initiative : AggregateRoot<Guid>
{
    private Initiative()
    {
    }

    private Initiative(
        Guid id,
        Guid clientRequestId,
        Guid ownerUserId,
        string title,
        string summary,
        string category,
        string city,
        decimal requestedBudget,
        int targetBeneficiaries,
        DateTimeOffset createdUtc)
        : base(id)
    {
        ClientRequestId = clientRequestId;
        OwnerUserId = ownerUserId;
        Title = title;
        Summary = summary;
        Category = category;
        City = city;
        RequestedBudget = requestedBudget;
        TargetBeneficiaries = targetBeneficiaries;
        Status = InitiativeStatuses.Submitted;
        CreatedUtc = createdUtc;
        UpdatedUtc = createdUtc;
    }

    public Guid ClientRequestId { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public decimal RequestedBudget { get; private set; }

    public int TargetBeneficiaries { get; private set; }

    public string Status { get; private set; } = InitiativeStatuses.Submitted;

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset UpdatedUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Result<Initiative> Create(
        Guid clientRequestId,
        Guid ownerUserId,
        string? title,
        string? summary,
        string? category,
        string? city,
        decimal requestedBudget,
        int targetBeneficiaries,
        DateTimeOffset createdUtc)
    {
        var normalizedTitle = Normalize(title);
        var normalizedSummary = Normalize(summary);
        var normalizedCategory = Normalize(category);
        var normalizedCity = Normalize(city);

        if (clientRequestId == Guid.Empty)
            return Result<Initiative>.Failure(InitiativeErrors.InvalidClientRequestId);

        if (ownerUserId == Guid.Empty)
            return Result<Initiative>.Failure(InitiativeErrors.InvalidOwner);

        if (normalizedTitle.Length is < 4 or > 140)
            return Result<Initiative>.Failure(InitiativeErrors.InvalidTitle);

        if (normalizedSummary.Length is < 30 or > 1800)
            return Result<Initiative>.Failure(InitiativeErrors.InvalidSummary);

        if (normalizedCategory.Length is < 2 or > 80)
            return Result<Initiative>.Failure(InitiativeErrors.InvalidCategory);

        if (normalizedCity.Length is < 2 or > 80)
            return Result<Initiative>.Failure(InitiativeErrors.InvalidCity);

        if (requestedBudget is < 0 or > 100_000_000)
            return Result<Initiative>.Failure(InitiativeErrors.InvalidBudget);

        if (targetBeneficiaries is < 1 or > 10_000_000)
            return Result<Initiative>.Failure(InitiativeErrors.InvalidBeneficiaries);

        var initiative = new Initiative(
            Guid.NewGuid(),
            clientRequestId,
            ownerUserId,
            normalizedTitle,
            normalizedSummary,
            normalizedCategory,
            normalizedCity,
            requestedBudget,
            targetBeneficiaries,
            createdUtc);

        initiative.RaiseDomainEvent(new InitiativeSubmitted(
            initiative.Id,
            initiative.OwnerUserId,
            initiative.CreatedUtc));

        return Result<Initiative>.Success(initiative);
    }

    public Result Review(
        string? decision,
        Guid reviewerUserId,
        string? notes,
        DateTimeOffset reviewedUtc)
    {
        var normalizedDecision = decision?.Trim().ToLowerInvariant();
        var normalizedNotes = Normalize(notes);

        if (reviewerUserId == Guid.Empty)
            return Result.Failure(InitiativeErrors.InvalidReviewer);

        if (reviewerUserId == OwnerUserId)
            return Result.Failure(InitiativeErrors.SelfReviewNotAllowed);

        if (!InitiativeDecisions.IsValid(normalizedDecision))
            return Result.Failure(InitiativeErrors.InvalidDecision);

        if (!InitiativeWorkflow.Definition.TryResolve(
                Status,
                normalizedDecision!,
                out var transition))
        {
            return Result.Failure(InitiativeErrors.AlreadyReviewed);
        }

        if (normalizedNotes.Length > 1200)
            return Result.Failure(InitiativeErrors.InvalidReviewNotes);

        Status = transition.ToState;
        UpdatedUtc = reviewedUtc;

        RaiseDomainEvent(new InitiativeReviewed(
            Id,
            reviewerUserId,
            Status,
            reviewedUtc));

        return Result.Success();
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}

public sealed class InitiativeReview : Entity<Guid>
{
    private InitiativeReview()
    {
    }

    private InitiativeReview(
        Guid id,
        Guid initiativeId,
        Guid reviewerUserId,
        string decision,
        string notes,
        DateTimeOffset reviewedUtc)
        : base(id)
    {
        InitiativeId = initiativeId;
        ReviewerUserId = reviewerUserId;
        Decision = decision;
        Notes = notes;
        ReviewedUtc = reviewedUtc;
    }

    public Guid InitiativeId { get; private set; }

    public Guid ReviewerUserId { get; private set; }

    public string Decision { get; private set; } = string.Empty;

    public string Notes { get; private set; } = string.Empty;

    public DateTimeOffset ReviewedUtc { get; private set; }

    public static InitiativeReview Create(
        Guid initiativeId,
        Guid reviewerUserId,
        string decision,
        string? notes,
        DateTimeOffset reviewedUtc) =>
        new(
            Guid.NewGuid(),
            initiativeId,
            reviewerUserId,
            decision,
            notes?.Trim() ?? string.Empty,
            reviewedUtc);
}

public sealed record InitiativeSubmitted(
    Guid InitiativeId,
    Guid OwnerUserId,
    DateTimeOffset SubmittedUtc) : IDomainEvent;

public sealed record InitiativeReviewed(
    Guid InitiativeId,
    Guid ReviewerUserId,
    string Status,
    DateTimeOffset ReviewedUtc) : IDomainEvent;

public static class InitiativeErrors
{
    public static readonly Error InvalidClientRequestId = Error.Validation(
        "Athar.InvalidClientRequestId",
        "معرّف الطلب من الواجهة غير صالح.");

    public static readonly Error InvalidOwner = Error.Unauthorized(
        "Athar.InvalidOwner",
        "تعذر تحديد صاحب المبادرة.");

    public static readonly Error InvalidTitle = Error.Validation(
        "Athar.InvalidTitle",
        "عنوان المبادرة يجب أن يكون بين 4 و140 حرفًا.");

    public static readonly Error InvalidSummary = Error.Validation(
        "Athar.InvalidSummary",
        "وصف المبادرة يجب أن يكون بين 30 و1800 حرف.");

    public static readonly Error InvalidCategory = Error.Validation(
        "Athar.InvalidCategory",
        "تصنيف المبادرة غير صالح.");

    public static readonly Error InvalidCity = Error.Validation(
        "Athar.InvalidCity",
        "المدينة غير صالحة.");

    public static readonly Error InvalidBudget = Error.Validation(
        "Athar.InvalidBudget",
        "الميزانية المطلوبة خارج النطاق المسموح.");

    public static readonly Error InvalidBeneficiaries = Error.Validation(
        "Athar.InvalidBeneficiaries",
        "عدد المستفيدين غير صالح.");

    public static readonly Error InvalidReviewer = Error.Forbidden(
        "Athar.InvalidReviewer",
        "تعذر تحديد المراجع الإداري.");

    public static readonly Error SelfReviewNotAllowed = Error.Forbidden(
        "Athar.SelfReviewNotAllowed",
        "لا يمكن لمسؤول النظام مراجعة مبادرة يملكها هو.");

    public static readonly Error InvalidDecision = Error.Validation(
        "Athar.InvalidDecision",
        "القرار يجب أن يكون approve أو reject.");

    public static readonly Error InvalidReviewNotes = Error.Validation(
        "Athar.InvalidReviewNotes",
        "ملاحظات المراجعة لا يمكن أن تتجاوز 1200 حرف.");

    public static readonly Error AlreadyReviewed = Error.Conflict(
        "Athar.AlreadyReviewed",
        "تمت مراجعة هذه المبادرة سابقًا.");

    public static readonly Error InitiativeNotFound = Error.NotFound(
        "Athar.InitiativeNotFound",
        "المبادرة المطلوبة غير موجودة.");

    public static readonly Error AuthenticationRequired = Error.Unauthorized(
        "Athar.AuthenticationRequired",
        "يجب تسجيل الدخول لإكمال العملية.");

    public static readonly Error AdministratorRequired = Error.Forbidden(
        "Athar.AdministratorRequired",
        "هذه العملية متاحة لمسؤول النظام فقط.");
}
