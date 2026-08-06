using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Events;
using FoundationKit.Application.Persistence;
using FoundationKit.Application.Results;
using FoundationKit.Infrastructure;
using FoundationKit.Infrastructure.Events;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.WebApi;
using FoundationKit.WebApi.Results;
using FoundationKit.Workbench.Application;
using FoundationKit.Workbench.Domain;
using FoundationKit.Workbench.Infrastructure;
using Microsoft.EntityFrameworkCore;

var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = webRoot
});

builder.Services.AddFoundationInfrastructure();
builder.Services.AddFoundationWebApi();

var connectionString = builder.Configuration.GetConnectionString("Workbench")
    ?? throw new InvalidOperationException(
        "Connection string 'Workbench' is required. See docs/WORKBENCH.md.");

builder.Services.AddDbContext<WorkbenchDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString, sqlServer =>
        sqlServer.MigrationsAssembly(typeof(WorkbenchDbContext).Assembly.FullName));
    options.AddInterceptors(
        serviceProvider.GetRequiredService<DomainEventsSaveChangesInterceptor>());
});

builder.Services.AddScoped<IRepository<BuildBrief, Guid>,
    EfRepository<BuildBrief, Guid, WorkbenchDbContext>>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork<WorkbenchDbContext>>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<CatalogService>();
builder.Services.AddScoped<IDomainEventHandler<BuildBriefCreated>, BuildBriefCreatedHandler>();

var app = builder.Build();
app.UseFoundationRequestPipeline();
app.UseDefaultFiles();
app.UseStaticFiles();

await DatabaseBootstrapper.MigrateAsync(app.Services, app.Logger, app.Lifetime.ApplicationStopping);

app.MapGet("/api/runtime", () => Results.Ok(new
{
    mode = "local",
    persistence = "sql-server",
    database = "FoundationKitWorkbench",
    contactName = "ALHassan ALShami"
}));

app.MapGet("/api/catalog", async (CatalogService catalog, CancellationToken cancellationToken) =>
    Results.Json(await catalog.ReadAsync(cancellationToken)));

app.MapGet("/api/health", async (WorkbenchDbContext dbContext, CancellationToken cancellationToken) =>
{
    var connected = await dbContext.Database.CanConnectAsync(cancellationToken);
    if (!connected)
    {
        return Results.Json(
            new { status = "unhealthy", database = "sql-server" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new { status = "healthy", database = "sql-server" });
});

app.MapPost("/api/build-briefs", async (
    BuildBriefRequest request,
    IRepository<BuildBrief, Guid> repository,
    IUnitOfWork unitOfWork,
    IClock clock,
    CatalogService catalog,
    CancellationToken cancellationToken) =>
{
    var knownCapabilities = await catalog.ReadCapabilityIdsAsync(cancellationToken);
    var unknownCapabilities = (request.SelectedCapabilityIds ?? [])
        .Where(id => !knownCapabilities.Contains(id))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (unknownCapabilities.Length > 0)
    {
        return Error.Validation(
            "BuildBrief.UnknownCapability",
            $"Unknown capability ids: {string.Join(", ", unknownCapabilities)}").ToProblem();
    }

    var result = BuildBrief.Create(
        request.ProjectName,
        request.ProjectType,
        request.Audience,
        request.Goal,
        request.SelectedCapabilityIds,
        request.Priorities,
        request.Notes,
        clock.UtcNow);

    if (result.IsFailure)
        return result.ToHttpResult(_ => Results.NoContent());

    var brief = result.Value;
    await repository.AddAsync(brief, cancellationToken);
    await unitOfWork.SaveChangesAsync(cancellationToken);

    var response = BuildBriefResponse.From(brief);
    return Results.Created($"/api/build-briefs/{brief.Id}", response);
});

app.MapGet("/api/build-briefs/{id:guid}", async (
    Guid id,
    IRepository<BuildBrief, Guid> repository,
    CancellationToken cancellationToken) =>
{
    var brief = await repository.GetByIdAsync(id, cancellationToken);
    return brief is null
        ? Results.NotFound()
        : Results.Ok(BuildBriefResponse.From(brief));
});

app.MapFallbackToFile("index.html");
app.Run();

public partial class Program
{
}
