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
using FoundationKit.Workbench.Contracts;
using FoundationKit.Workbench.Domain;
using FoundationKit.Workbench.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFoundationInfrastructure();
builder.Services.AddFoundationWebApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FoundationKit Workbench API",
        Version = "v1",
        Description = "The official local API consumed by Blazor WebAssembly and reusable from Postman."
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalWorkbenchClient", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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
app.UseCors("LocalWorkbenchClient");
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FoundationKit Workbench API v1");
    options.DocumentTitle = "FoundationKit Workbench API";
});
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

await DatabaseBootstrapper.MigrateAsync(
    app.Services,
    app.Logger,
    app.Lifetime.ApplicationStopping);

var api = app.MapGroup("/api")
    .WithTags("FoundationKit Workbench");

api.MapGet("/runtime", () => TypedResults.Ok(new RuntimeResponse(
        "local",
        "sql-server",
        "FoundationKitWorkbench",
        "ALHassan ALShami")))
    .WithName("GetWorkbenchRuntime")
    .WithSummary("Returns the active local runtime and persistence mode.")
    .Produces<RuntimeResponse>();

api.MapGet("/catalog", async (
        CatalogService catalog,
        CancellationToken cancellationToken) =>
        Results.Json(await catalog.ReadAsync(cancellationToken)))
    .WithName("GetFoundationKitCatalog")
    .WithSummary("Returns the canonical implemented FoundationKit capability catalog.")
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
    .WithSummary("Checks API and SQL Server connectivity.")
    .Produces<HealthResponse>()
    .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

api.MapPost("/build-briefs", async (
        BuildBriefRequest request,
        IRepository<BuildBrief, Guid> repository,
        IUnitOfWork unitOfWork,
        IClock clock,
        CatalogService catalog,
        CancellationToken cancellationToken) =>
    {
        var knownCapabilities = await catalog.ReadCapabilityIdsAsync(cancellationToken);
        var unknownCapabilities = request.SelectedCapabilityIds
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

        var response = ToResponse(brief);
        return Results.Created($"/api/build-briefs/{brief.Id:D}", response);
    })
    .WithName("CreateBuildBrief")
    .WithSummary("Creates and persists a project brief using the shared request contract.")
    .Accepts<BuildBriefRequest>("application/json")
    .Produces<BuildBriefResponse>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest);

api.MapGet("/build-briefs/{id:guid}", async (
        Guid id,
        IRepository<BuildBrief, Guid> repository,
        CancellationToken cancellationToken) =>
    {
        var brief = await repository.GetByIdAsync(id, cancellationToken);
        return brief is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(brief));
    })
    .WithName("GetBuildBrief")
    .WithSummary("Gets a previously persisted project brief by identifier.")
    .Produces<BuildBriefResponse>()
    .Produces(StatusCodes.Status404NotFound);

app.MapFallbackToFile("index.html");
app.Run();

static BuildBriefResponse ToResponse(BuildBrief brief) => new(
    brief.Id,
    brief.ProjectName,
    brief.ProjectType,
    brief.Audience,
    brief.Goal,
    brief.SelectedCapabilityIds,
    brief.Priorities,
    brief.Notes,
    brief.CreatedUtc,
    ContactLinkBuilder.Build(brief));

public partial class Program
{
}
