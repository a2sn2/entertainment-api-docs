using EntertainmentDocs.Domain.Documents;

namespace EntertainmentDocs.Domain.Tests;

public sealed class DocumentationDocumentTests
{
    [Fact]
    public void Publish_requires_review_state()
    {
        var now = DateTimeOffset.UtcNow;
        var document = DocumentationDocument.Create("API-ENT-DOC-001", "entertainment", "Entertainment API", Guid.NewGuid(), now);
        document.AddVersion("1.0", "content", Guid.NewGuid(), now);

        Assert.Throws<InvalidOperationException>(() => document.Publish(now));
    }

    [Fact]
    public void Reviewed_document_can_be_published()
    {
        var now = DateTimeOffset.UtcNow;
        var document = DocumentationDocument.Create("API-ENT-DOC-001", "entertainment", "Entertainment API", Guid.NewGuid(), now);
        document.AddVersion("1.0", "content", Guid.NewGuid(), now);
        document.SubmitForReview(now);
        document.Publish(now);

        Assert.Equal(DocumentStatus.Published, document.Status);
    }
}
