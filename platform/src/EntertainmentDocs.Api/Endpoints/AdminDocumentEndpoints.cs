using EntertainmentDocs.Api.Authorization;
using EntertainmentDocs.Application.Documents;
using EntertainmentDocs.Contracts.Common;
using EntertainmentDocs.Contracts.Documents;

namespace EntertainmentDocs.Api.Endpoints;

public static class AdminDocumentEndpoints
{
    public static IEndpointRouteBuilder MapAdminDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/documents")
            .RequireAuthorization()
            .RequireRateLimiting("api")
            .WithTags("Admin Documents");

        group.MapPost("/", async (CreateDocumentRequest request, DocumentService service, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request.Reference, request.Slug, request.Title, ct);
            return result.Succeeded
                ? Results.Created($"/api/v1/admin/documents/{result.Value}", new CreatedDocumentResponse(result.Value))
                : Results.BadRequest(new ApiErrorResponse(result.Error ?? "Document creation failed."));
        })
        .RequireAuthorization(Policies.ManageContent)
        .WithName("CreateDocument")
        .WithSummary("Create a draft documentation record.")
        .Produces<CreatedDocumentResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/versions", async (Guid id, AddDocumentVersionRequest request, DocumentService service, CancellationToken ct) =>
        {
            var result = await service.AddVersionAsync(id, request.Version, request.Content, ct);
            return result.Succeeded
                ? Results.Ok(new CreatedDocumentVersionResponse(result.Value))
                : Results.BadRequest(new ApiErrorResponse(result.Error ?? "Document version creation failed."));
        })
        .RequireAuthorization(Policies.ManageContent)
        .WithName("AddDocumentVersion")
        .WithSummary("Add a version and its documentation content to an existing document.")
        .Produces<CreatedDocumentVersionResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/submit-review", async (Guid id, DocumentService service, CancellationToken ct) =>
        {
            var result = await service.SubmitForReviewAsync(id, ct);
            return result.Succeeded
                ? Results.NoContent()
                : Results.BadRequest(new ApiErrorResponse(result.Error ?? "Submit for review failed."));
        })
        .RequireAuthorization(Policies.ManageContent)
        .WithName("SubmitDocumentForReview")
        .WithSummary("Move a draft document into the review workflow.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/publish", async (Guid id, DocumentService service, CancellationToken ct) =>
        {
            var result = await service.PublishAsync(id, ct);
            return result.Succeeded
                ? Results.NoContent()
                : Results.BadRequest(new ApiErrorResponse(result.Error ?? "Publish failed."));
        })
        .RequireAuthorization(Policies.PublishContent)
        .WithName("PublishDocument")
        .WithSummary("Publish a reviewed document for public retrieval.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        return app;
    }
}
