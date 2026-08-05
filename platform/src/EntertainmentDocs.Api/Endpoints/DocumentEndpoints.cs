using EntertainmentDocs.Application.Documents;
using EntertainmentDocs.Contracts.Documents;
using FoundationKit.Application.Messaging;
using FoundationKit.WebApi.Results;

namespace EntertainmentDocs.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/documents")
            .WithTags("Documents")
            .RequireRateLimiting("api");

        group.MapGet("/", async (
            IQueryHandler<ListPublishedDocumentsQuery, IReadOnlyList<DocumentSummaryDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new ListPublishedDocumentsQuery(), cancellationToken);
            return result.ToHttpResult(documents => Results.Ok(documents
                .Select(document => new DocumentSummaryResponse(
                    document.Id,
                    document.Reference,
                    document.Slug,
                    document.Title,
                    document.Status,
                    document.UpdatedAt))
                .ToArray()));
        })
        .WithName("ListPublishedDocuments")
        .WithSummary("List all currently published documentation records.")
        .Produces<IReadOnlyList<DocumentSummaryResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{slug}", async (
            string slug,
            IQueryHandler<GetPublishedDocumentQuery, DocumentDetailsDto> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new GetPublishedDocumentQuery(slug), cancellationToken);
            return result.ToHttpResult(document => Results.Ok(new DocumentDetailsResponse(
                document.Id,
                document.Reference,
                document.Slug,
                document.Title,
                document.Status,
                document.Version,
                document.Content,
                document.UpdatedAt)));
        })
        .WithName("GetPublishedDocument")
        .WithSummary("Get the latest published version of one document by slug.")
        .Produces<DocumentDetailsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
