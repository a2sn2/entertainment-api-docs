using FoundationKit.WebApi.Results;
using Madar.Api.Security;
using Madar.Application.Cases;
using Madar.Contracts.Cases;
using Madar.Contracts.Organization;

namespace Madar.Api;

public static class DepartmentEndpoints
{
    public static IEndpointRouteBuilder MapMadarDepartmentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                DepartmentRoutes.Root,
                async (
                    ICaseRoutingManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.ListDepartmentsAsync(cancellationToken))
                        .ToHttpResult(Results.Ok))
            .RequireAuthorization()
            .WithTags("Departments")
            .WithName("ListMadarDepartments")
            .Produces<IReadOnlyList<DepartmentDto>>();

        endpoints.MapGet(
                "/api/departments/{departmentId:guid}/queue",
                async (
                    Guid departmentId,
                    ICaseRoutingManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.GetQueueAsync(
                        departmentId,
                        cancellationToken))
                    .ToHttpResult(Results.Ok))
            .RequireAuthorization()
            .WithTags("Departments")
            .WithName("GetMadarDepartmentQueue")
            .Produces<DepartmentQueueDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost(
                "/api/cases/{caseId:guid}/route",
                async (
                    Guid caseId,
                    RouteCaseRequest request,
                    ICaseRoutingManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.RouteAsync(
                        caseId,
                        request,
                        cancellationToken))
                    .ToHttpResult(Results.Ok))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithTags("Case Routing")
            .WithName("RouteMadarCase")
            .Produces<CaseDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPost(
                "/api/cases/{caseId:guid}/claim",
                async (
                    Guid caseId,
                    ICaseRoutingManager manager,
                    CancellationToken cancellationToken) =>
                    (await manager.ClaimAsync(
                        caseId,
                        cancellationToken))
                    .ToHttpResult(Results.Ok))
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .RequireRateLimiting("write")
            .WithTags("Case Routing")
            .WithName("ClaimMadarCase")
            .Produces<CaseDto>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }
}
