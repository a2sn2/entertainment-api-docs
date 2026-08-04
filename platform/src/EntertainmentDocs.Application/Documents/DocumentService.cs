using EntertainmentDocs.Application.Abstractions;
using EntertainmentDocs.Application.Common;
using EntertainmentDocs.Domain.Documents;

namespace EntertainmentDocs.Application.Documents;

public sealed class DocumentService(
    IDocumentRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<Result<Guid>> CreateAsync(string reference, string slug, string title, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) return Result<Guid>.Failure("Authentication is required.");
        if (await repository.ReferenceExistsAsync(reference, ct)) return Result<Guid>.Failure("Document reference already exists.");
        if (await repository.SlugExistsAsync(slug, ct)) return Result<Guid>.Failure("Document slug already exists.");

        var document = DocumentationDocument.Create(reference, slug, title, userId, clock.UtcNow);
        await repository.AddAsync(document, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Success(document.Id);
    }

    public async Task<Result<Guid>> AddVersionAsync(Guid id, string version, string content, CancellationToken ct)
    {
        if (currentUser.UserId is not Guid userId) return Result<Guid>.Failure("Authentication is required.");
        var document = await repository.GetAsync(id, ct);
        if (document is null) return Result<Guid>.Failure("Document was not found.");
        var item = document.AddVersion(version, content, userId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Success(item.Id);
    }

    public async Task<Result<bool>> SubmitForReviewAsync(Guid id, CancellationToken ct)
    {
        var document = await repository.GetAsync(id, ct);
        if (document is null) return Result<bool>.Failure("Document was not found.");
        document.SubmitForReview(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> PublishAsync(Guid id, CancellationToken ct)
    {
        var document = await repository.GetAsync(id, ct);
        if (document is null) return Result<bool>.Failure("Document was not found.");
        document.Publish(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<IReadOnlyList<DocumentSummaryDto>> ListPublishedAsync(CancellationToken ct) =>
        (await repository.ListPublishedAsync(ct))
            .Select(x => new DocumentSummaryDto(x.Id, x.Reference, x.Slug, x.Title, x.Status.ToString(), x.UpdatedAt))
            .ToArray();

    public async Task<DocumentDetailsDto?> GetPublishedAsync(string slug, CancellationToken ct)
    {
        var document = await repository.GetPublishedBySlugAsync(slug, ct);
        var latest = document?.Versions.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        return document is null || latest is null ? null :
            new(document.Id, document.Reference, document.Slug, document.Title, document.Status.ToString(), latest.Version, latest.Content, document.UpdatedAt);
    }
}
