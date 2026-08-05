using EntertainmentDocs.Domain.Documents;
using FoundationKit.Application.Persistence;

namespace EntertainmentDocs.Application.Abstractions;

public interface IDocumentRepository : IRepository<DocumentationDocument, Guid>
{
    Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);
    Task<DocumentationDocument?> GetWithVersionsAsync(Guid id, CancellationToken cancellationToken);
    Task<DocumentationDocument?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentationDocument>> ListPublishedAsync(CancellationToken cancellationToken);
    Task AddVersionAsync(DocumentVersion version, CancellationToken cancellationToken);
}
