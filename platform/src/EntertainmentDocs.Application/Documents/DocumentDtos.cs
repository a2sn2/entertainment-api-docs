namespace EntertainmentDocs.Application.Documents;

public sealed record DocumentSummaryDto(Guid Id, string Reference, string Slug, string Title, string Status, DateTimeOffset UpdatedAt);
public sealed record DocumentDetailsDto(Guid Id, string Reference, string Slug, string Title, string Status, string Version, string Content, DateTimeOffset UpdatedAt);
