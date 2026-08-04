using EntertainmentDocs.Application.Documents;

namespace EntertainmentDocs.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/documents").WithTags("Documents").RequireRateLimiting("api");
        group.MapGet("/", async (DocumentService service, CancellationToken ct) =>
            Results.Ok(await service.ListPublishedAsync(ct)));
        group.MapGet("/{slug}", async (string slug, DocumentService service, CancellationToken ct) =>
            await service.GetPublishedAsync(slug, ct) is { } document ? Results.Ok(document) : Results.NotFound());
        return app;
    }
}
