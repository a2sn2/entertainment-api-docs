namespace Madar.Contracts.Cases;

public sealed record AddCaseCommentRequest(string Body);

public sealed record CaseCommentDto(
    Guid Id,
    Guid CaseId,
    Guid AuthorUserId,
    string AuthorDisplayName,
    string Body,
    DateTimeOffset CreatedUtc);

public static class CaseCommentRoutes
{
    public static string ForCase(Guid caseId) =>
        $"{CaseRoutes.ById(caseId)}/comments";
}
