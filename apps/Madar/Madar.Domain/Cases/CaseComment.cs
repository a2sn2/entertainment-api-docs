using FoundationKit.Application.Results;

namespace Madar.Domain.Cases;

public sealed class CaseComment
{
    private CaseComment()
    {
    }

    private CaseComment(
        Guid id,
        Guid caseId,
        Guid authorUserId,
        string body,
        DateTimeOffset createdUtc)
    {
        Id = id;
        CaseId = caseId;
        AuthorUserId = authorUserId;
        Body = body;
        CreatedUtc = createdUtc;
    }

    public Guid Id { get; private set; }

    public Guid CaseId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Result<CaseComment> Create(
        Guid caseId,
        Guid authorUserId,
        string? body,
        DateTimeOffset createdUtc)
    {
        if (caseId == Guid.Empty)
            return Result<CaseComment>.Failure(CaseCommentErrors.InvalidCase);

        if (authorUserId == Guid.Empty)
            return Result<CaseComment>.Failure(CaseCommentErrors.InvalidAuthor);

        var normalizedBody = body?.Trim() ?? string.Empty;
        if (normalizedBody.Length is < 1 or > 2000)
            return Result<CaseComment>.Failure(CaseCommentErrors.InvalidBody);

        return Result<CaseComment>.Success(
            new CaseComment(
                Guid.NewGuid(),
                caseId,
                authorUserId,
                normalizedBody,
                createdUtc));
    }
}

public static class CaseCommentErrors
{
    public static readonly Error InvalidCase = Error.Validation(
        "Madar.CommentInvalidCase",
        "تعذر تحديد الحالة المرتبطة بالتعليق.");

    public static readonly Error InvalidAuthor = Error.Unauthorized(
        "Madar.CommentInvalidAuthor",
        "تعذر تحديد كاتب التعليق.");

    public static readonly Error InvalidBody = Error.Validation(
        "Madar.CommentInvalidBody",
        "نص التعليق مطلوب ويجب ألا يتجاوز 2000 حرف.");
}
