using EntertainmentDocs.Api.Authorization;
using EntertainmentDocs.Application.Documents;
using EntertainmentDocs.Contracts.Documents;
using FoundationKit.Application.Messaging;
using FoundationKit.WebApi.Results;

namespace EntertainmentDocs.Api.Endpoints;

public static class AdminDocumentEndpoints
{
    public static IEndpointRouteBuilder MapAdminDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/documents")
            .RequireAuthorization()
            .RequireRateLimiting("api")
            .WithTags("Admin Documents");

        group.MapPost("/", async (
            CreateDocumentRequest request,
            ICommandHandler<CreateDocumentCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new CreateDocumentCommand(request.Reference, request.Slug, request.Title),
                cancellationToken);

            return result.ToHttpResult(id =>
                Results.Created($"/api/v1/admin/documents/{id}", new CreatedDocumentResponse(id)));
        })
        .RequireAuthorization(Policies.ManageContent)
        .WithName("CreateDocument")
        .WithSummary("Create a draft documentation record.")
        .Produces<CreatedDocumentResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/versions", async (
            Guid id,
            AddDocumentVersionRequest request,
            ICommandHandler<AddDocumentVersionCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new AddDocumentVersionCommand(id, request.Version, request.Content),
                cancellationToken);

            return result.ToHttpResult(versionId => Results.Ok(new CreatedDocumentVersionResponse(versionId)));
        })
        .RequireAuthorization(Policies.ManageContent)
        .WithName("AddDocumentVersion")
        .WithSummary("Add a version and its documentation content to an existing document.")
        .Produces<CreatedDocumentVersionResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/submit-review", async (
            Guid id,
            ICommandHandler<SubmitDocumentForReviewCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new SubmitDocumentForReviewCommand(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Policies.ManageContent)
        .WithName("SubmitDocumentForReview")
        .WithSummary("Move a draft document into the review workflow.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/publish", async (
            Guid id,
            ICommandHandler<PublishDocumentCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new PublishDocumentCommand(id), cancellationToken);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Policies.PublishContent)
        .WithName("PublishDocument")
        .WithSummary("Publish a reviewed document for public retrieval.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
