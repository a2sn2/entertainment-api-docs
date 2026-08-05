using EntertainmentDocs.Application.Abstractions;
using FoundationKit.Application.Messaging;
using FoundationKit.Application.Results;

namespace EntertainmentDocs.Application.Documents;

public sealed record ListPublishedDocumentsQuery : IQuery<IReadOnlyList<DocumentSummaryDto>>;

public sealed class ListPublishedDocumentsQueryHandler(IDocumentRepository repository)
    : IQueryHandler<ListPublishedDocumentsQuery, IReadOnlyList<DocumentSummaryDto>>
{
    public async Task<Result<IReadOnlyList<DocumentSummaryDto>>> HandleAsync(
        ListPublishedDocumentsQuery query,
        CancellationToken cancellationToken = default)
    {
        var documents = await repository.ListPublishedAsync(cancellationToken);
        IReadOnlyList<DocumentSummaryDto> result = documents
            .Select(document => new DocumentSummaryDto(
                document.Id,
                document.Reference,
                document.Slug,
                document.Title,
                document.Status.ToString(),
                document.UpdatedAt))
            .ToArray();

        return Result<IReadOnlyList<DocumentSummaryDto>>.Success(result);
    }
}
