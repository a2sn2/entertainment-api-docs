using EntertainmentDocs.Domain.Common;

namespace EntertainmentDocs.Domain.Documents;

public sealed class DocumentationDocument : AggregateRoot
{
    private readonly List<DocumentVersion> _versions = [];
    private DocumentationDocument() { }

    private DocumentationDocument(Guid id, string reference, string slug, string title, Guid ownerId, DateTimeOffset createdAt)
        : base(id)
    {
        Reference = Require(reference, nameof(reference));
        Slug = Require(slug, nameof(slug)).ToLowerInvariant();
        Title = Require(title, nameof(title));
        OwnerId = ownerId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Status = DocumentStatus.Draft;
    }

    public string Reference { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public IReadOnlyCollection<DocumentVersion> Versions => _versions.AsReadOnly();

    public static DocumentationDocument Create(string reference, string slug, string title, Guid ownerId, DateTimeOffset now) =>
        new(Guid.NewGuid(), reference, slug, title, ownerId, now);

    public DocumentVersion AddVersion(string version, string content, Guid authorId, DateTimeOffset now)
    {
        if (Status is DocumentStatus.Archived)
            throw new InvalidOperationException("Archived documents cannot receive new versions.");

        var item = new DocumentVersion(Guid.NewGuid(), Id, Require(version, nameof(version)), Require(content, nameof(content)), authorId, now);
        _versions.Add(item);
        UpdatedAt = now;
        if (Status is DocumentStatus.Published) Status = DocumentStatus.Draft;
        return item;
    }

    public void SubmitForReview(DateTimeOffset now)
    {
        if (_versions.Count == 0) throw new InvalidOperationException("A document requires at least one version.");
        if (Status is not DocumentStatus.Draft) throw new InvalidOperationException("Only draft documents can enter review.");
        Status = DocumentStatus.InReview;
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        if (Status is not DocumentStatus.InReview) throw new InvalidOperationException("Only reviewed documents can be published.");
        Status = DocumentStatus.Published;
        PublishedAt = now;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = DocumentStatus.Archived;
        UpdatedAt = now;
    }

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();
}
