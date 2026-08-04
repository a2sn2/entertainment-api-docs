using EntertainmentDocs.Api.Authorization;
using EntertainmentDocs.Application.Documents;

namespace EntertainmentDocs.Api.Endpoints;

public static class AdminDocumentEndpoints
{
    public static IEndpointRouteBuilder MapAdminDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/documents").RequireAuthorization().RequireRateLimiting("api").WithTags("Admin Documents");

        group.MapPost("/", async (CreateDocumentRequest request, DocumentService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request.Reference, request.Slug, request.Title, ct);
            return result.Succeeded ? Results.Created($"/api/v1/admin/documents/{result.Value}", new { id = result.Value }) : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Policies.ManageContent);

        group.MapPost("/{id:guid}/versions", async (Guid id, AddVersionRequest request, DocumentService service, CancellationToken ct) =>
        {
            var result = await service.AddVersionAsync(id, request.Version, request.Content, ct);
            return result.Succeeded ? Results.Ok(new { versionId = result.Value }) : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Policies.ManageContent);

        group.MapPost("/{id:guid}/submit-review", async (Guid id, DocumentService service, CancellationToken ct) =>
        {
            var result = await service.SubmitForReviewAsync(id, ct);
            return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Policies.ManageContent);

        group.MapPost("/{id:guid}/publish", async (Guid id, DocumentService service, CancellationToken ct) =>
        {
            var result = await service.PublishAsync(id, ct);
            return result.Succeeded ? Results.NoContent() : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(Policies.PublishContent);

        return app;
    }

    public sealed record CreateDocumentRequest(string Reference, string Slug, string Title);
    public sealed record AddVersionRequest(string Version, string Content);
}
