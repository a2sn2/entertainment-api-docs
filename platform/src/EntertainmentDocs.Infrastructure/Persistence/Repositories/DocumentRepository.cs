using EntertainmentDocs.Application.Abstractions;
using EntertainmentDocs.Domain.Documents;
using FoundationKit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EntertainmentDocs.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository
    : EfRepository<DocumentationDocument, Guid, AppDbContext>, IDocumentRepository
{
    private readonly AppDbContext _dbContext;

    public DocumentRepository(AppDbContext dbContext) : base(dbContext) =>
        _dbContext = dbContext;

    public Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken) =>
        _dbContext.Documents.AnyAsync(document => document.Reference == reference, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken) =>
        _dbContext.Documents.AnyAsync(document => document.Slug == slug.ToLowerInvariant(), cancellationToken);

    public Task<DocumentationDocument?> GetWithVersionsAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Documents
            .Include(document => document.Versions)
            .SingleOrDefaultAsync(document => document.Id == id, cancellationToken);

    public Task<DocumentationDocument?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken) =>
        _dbContext.Documents
            .AsNoTracking()
            .Include(document => document.Versions)
            .SingleOrDefaultAsync(
                document => document.Slug == slug.ToLowerInvariant() && document.Status == DocumentStatus.Published,
                cancellationToken);

    public async Task<IReadOnlyList<DocumentationDocument>> ListPublishedAsync(CancellationToken cancellationToken) =>
        await _dbContext.Documents
            .AsNoTracking()
            .Where(document => document.Status == DocumentStatus.Published)
            .OrderBy(document => document.Title)
            .ToListAsync(cancellationToken);

    public Task AddVersionAsync(DocumentVersion version, CancellationToken cancellationToken) =>
        _dbContext.DocumentVersions.AddAsync(version, cancellationToken).AsTask();
}
