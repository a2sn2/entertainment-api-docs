namespace EntertainmentDocs.Contracts.Documents;

public sealed record CreateDocumentRequest(
    string Reference,
    string Slug,
    string Title);

public sealed record AddDocumentVersionRequest(
    string Version,
    string Content);

public sealed record CreatedDocumentResponse(Guid Id);

public sealed record CreatedDocumentVersionResponse(Guid VersionId);

public sealed record DocumentSummaryResponse(
    Guid Id,
    string Reference,
    string Slug,
    string Title,
    string Status,
    DateTimeOffset UpdatedAt);

public sealed record DocumentDetailsResponse(
    Guid Id,
    string Reference,
    string Slug,
    string Title,
    string Status,
    string Version,
    string Content,
    DateTimeOffset UpdatedAt);
