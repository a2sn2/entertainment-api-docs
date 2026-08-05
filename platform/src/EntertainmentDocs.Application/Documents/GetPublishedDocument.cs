using EntertainmentDocs.Application.Abstractions;
using FoundationKit.Application.Messaging;
using FoundationKit.Application.Results;

namespace EntertainmentDocs.Application.Documents;

public sealed record GetPublishedDocumentQuery(string Slug) : IQuery<DocumentDetailsDto>;

public sealed class GetPublishedDocumentQueryHandler(IDocumentRepository repository)
    : IQueryHandler<GetPublishedDocumentQuery, DocumentDetailsDto>
{
    public async Task<Result<DocumentDetailsDto>> HandleAsync(
        GetPublishedDocumentQuery query,
        CancellationToken cancellationToken = default)
    {
        var document = await repository.GetPublishedBySlugAsync(query.Slug, cancellationToken);
        var latestVersion = document?.Versions.OrderByDescending(version => version.CreatedAt).FirstOrDefault();
        if (document is null || latestVersion is null)
            return Result<DocumentDetailsDto>.Failure(DocumentErrors.NotFound);

        return Result<DocumentDetailsDto>.Success(new DocumentDetailsDto(
            document.Id,
            document.Reference,
            document.Slug,
            document.Title,
            document.Status.ToString(),
            latestVersion.Version,
            latestVersion.Content,
            document.UpdatedAt));
    }
}
