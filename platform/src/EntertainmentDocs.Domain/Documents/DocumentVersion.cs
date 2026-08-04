using EntertainmentDocs.Domain.Common;

namespace EntertainmentDocs.Domain.Documents;

public sealed class DocumentVersion : Entity
{
    private DocumentVersion() { }
    internal DocumentVersion(Guid id, Guid documentId, string version, string content, Guid authorId, DateTimeOffset createdAt)
        : base(id)
    {
        DocumentId = documentId;
        Version = version;
        Content = content;
        AuthorId = authorId;
        CreatedAt = createdAt;
    }

    public Guid DocumentId { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public Guid AuthorId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
