using EntertainmentDocs.Application.Abstractions;
using EntertainmentDocs.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace EntertainmentDocs.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository(AppDbContext dbContext) : IDocumentRepository
{
    public Task<bool> ReferenceExistsAsync(string reference, CancellationToken ct) =>
        dbContext.Documents.AnyAsync(x => x.Reference == reference, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) =>
        dbContext.Documents.AnyAsync(x => x.Slug == slug.ToLower(), ct);

    public Task<DocumentationDocument?> GetAsync(Guid id, CancellationToken ct) =>
        dbContext.Documents.Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<DocumentationDocument?> GetPublishedBySlugAsync(string slug, CancellationToken ct) =>
        dbContext.Documents.AsNoTracking().Include(x => x.Versions)
            .SingleOrDefaultAsync(x => x.Slug == slug.ToLower() && x.Status == DocumentStatus.Published, ct);

    public async Task<IReadOnlyList<DocumentationDocument>> ListPublishedAsync(CancellationToken ct) =>
        await dbContext.Documents.AsNoTracking()
            .Where(x => x.Status == DocumentStatus.Published)
            .OrderBy(x => x.Title)
            .ToListAsync(ct);

    public Task AddAsync(DocumentationDocument document, CancellationToken ct) =>
        dbContext.Documents.AddAsync(document, ct).AsTask();

    public Task AddVersionAsync(DocumentVersion version, CancellationToken ct) =>
        dbContext.Set<DocumentVersion>().AddAsync(version, ct).AsTask();
}
