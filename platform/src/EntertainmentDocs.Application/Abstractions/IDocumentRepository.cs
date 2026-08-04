using EntertainmentDocs.Domain.Documents;

namespace EntertainmentDocs.Application.Abstractions;

public interface IDocumentRepository
{
    Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);
    Task<DocumentationDocument?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<DocumentationDocument?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentationDocument>> ListPublishedAsync(CancellationToken cancellationToken);
    Task AddAsync(DocumentationDocument document, CancellationToken cancellationToken);
}
