using FoundationKit.Application.Persistence;
using FoundationKit.WebApi.Results;
using FoundationKit.Workbench.Application.User;
using FoundationKit.Workbench.Contracts.User;
using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Endpoints;

public static class UserPortalEndpoints
{
    public static IEndpointRouteBuilder MapUserPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/user")
            .WithTags("User full stack");

        api.MapPost("/requests", async (
                CreateUserRequest request,
                CreateUserRequestUseCase useCase,
                CancellationToken cancellationToken) =>
            {
                var result = await useCase.ExecuteAsync(request, cancellationToken);
                if (result.IsFailure)
                    return result.ToHttpResult(_ => Results.NoContent());

                var response = WorkbenchContractMapper.ToUserResponse(result.Value);
                return Results.Created($"/{Contracts.ApiRoutes.User.Request(result.Value.Id)}", response);
            })
            .WithName("CreateUserRequest")
            .WithSummary("Creates a user request and sends it into the shared admin workflow.")
            .Accepts<CreateUserRequest>("application/json")
            .Produces<UserRequestResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        api.MapGet("/requests/{id:guid}", async (
                Guid id,
                IRepository<BuildBrief, Guid> repository,
                CancellationToken cancellationToken) =>
            {
                var brief = await repository.GetByIdAsync(id, cancellationToken);
                return brief is null
                    ? Results.NotFound()
                    : Results.Ok(WorkbenchContractMapper.ToUserResponse(brief));
            })
            .WithName("GetUserRequest")
            .WithSummary("Returns the current request status visible to the user portal.")
            .Produces<UserRequestResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
