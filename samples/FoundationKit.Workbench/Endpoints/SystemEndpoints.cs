using FoundationKit.Workbench.Contracts;
using FoundationKit.Workbench.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Workbench.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api")
            .WithTags("Shared platform");

        api.MapGet("/runtime", () => TypedResults.Ok(new RuntimeResponse(
                "local",
                "sql-server",
                "FoundationKitWorkbench",
                "ALHassan ALShami")))
            .WithName("GetWorkbenchRuntime")
            .WithSummary("Returns the shared runtime used by both full-stack portals.")
            .Produces<RuntimeResponse>();

        api.MapGet("/catalog", async (
                CatalogService catalog,
                CancellationToken cancellationToken) =>
                Results.Json(await catalog.ReadAsync(cancellationToken)))
            .WithName("GetFoundationKitCatalog")
            .WithSummary("Returns the reusable FoundationKit capability catalog.")
            .Produces<CatalogResponse>();

        api.MapGet("/health", async (
                WorkbenchDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var connected = await dbContext.Database.CanConnectAsync(cancellationToken);
                return connected
                    ? Results.Ok(new HealthResponse("healthy", "sql-server"))
                    : Results.Json(
                        new HealthResponse("unhealthy", "sql-server"),
                        statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .WithName("GetWorkbenchHealth")
            .WithSummary("Checks the shared API host and SQL Server connection.")
            .Produces<HealthResponse>()
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
}
