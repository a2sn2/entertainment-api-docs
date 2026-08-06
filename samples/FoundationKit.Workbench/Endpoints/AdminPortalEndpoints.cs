using FoundationKit.WebApi.Results;
using FoundationKit.Workbench.Application.Admin;
using FoundationKit.Workbench.Contracts.Admin;

namespace FoundationKit.Workbench.Endpoints;

public static class AdminPortalEndpoints
{
    public static IEndpointRouteBuilder MapAdminPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/admin")
            .WithTags("Admin full stack");

        api.MapGet("/requests", async (
                string? status,
                IAdminQueueReader queueReader,
                CancellationToken cancellationToken) =>
                Results.Ok(await queueReader.ReadAsync(status, cancellationToken)))
            .WithName("GetAdminRequestQueue")
            .WithSummary("Returns the admin work queue, submitted by default.")
            .Produces<IReadOnlyList<AdminQueueItemResponse>>();

        api.MapPost("/requests/{id:guid}/review", async (
                Guid id,
                AdminReviewRequest request,
                ReviewUserRequestUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(id, request, cancellationToken);
                return result.IsFailure
                    ? result.ToHttpResult(_ => Results.NoContent())
                    : Results.Ok(WorkbenchContractMapper.ToAdminReviewResponse(result.Value));
            })
            .WithName("ReviewUserRequest")
            .WithSummary("Approves or rejects a user request and closes the shared workflow transition.")
            .Accepts<AdminReviewRequest>("application/json")
            .Produces<AdminReviewResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }
}
