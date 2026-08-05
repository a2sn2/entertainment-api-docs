using EntertainmentDocs.Application.Documents;
using EntertainmentDocs.Contracts.Documents;

namespace EntertainmentDocs.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/documents")
            .WithTags("Documents")
            .RequireRateLimiting("api");

        group.MapGet("/", async (DocumentService service, CancellationToken ct) =>
        {
            var documents = await service.ListPublishedAsync(ct);
            var response = documents
                .Select(document => new DocumentSummaryResponse(
                    document.Id,
                    document.Reference,
                    document.Slug,
                    document.Title,
                    document.Status,
                    document.UpdatedAt))
                .ToArray();

            return Results.Ok(response);
        })
        .WithName("ListPublishedDocuments")
        .WithSummary("List all currently published documentation records.")
        .Produces<IReadOnlyList<DocumentSummaryResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{slug}", async (string slug, DocumentService service, CancellationToken ct) =>
        {
            var document = await service.GetPublishedAsync(slug, ct);
            if (document is null)
                return Results.NotFound();

            return Results.Ok(new DocumentDetailsResponse(
                document.Id,
                document.Reference,
                document.Slug,
                document.Title,
                document.Status,
                document.Version,
                document.Content,
                document.UpdatedAt));
        })
        .WithName("GetPublishedDocument")
        .WithSummary("Get the latest published version of one document by slug.")
        .Produces<DocumentDetailsResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
