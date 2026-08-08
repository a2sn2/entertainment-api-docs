using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Events;
using FoundationKit.Application.Persistence;
using FoundationKit.Caching;
using FoundationKit.FeatureManagement;
using FoundationKit.Infrastructure;
using FoundationKit.Infrastructure.Events;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Localization;
using FoundationKit.Settings;
using FoundationKit.WebApi;
using FoundationKit.Workbench;
using FoundationKit.Workbench.Application;
using FoundationKit.Workbench.Application.Admin;
using FoundationKit.Workbench.Application.Shared;
using FoundationKit.Workbench.Application.User;
using FoundationKit.Workbench.Domain;
using FoundationKit.Workbench.Endpoints;
using FoundationKit.Workbench.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFoundationInfrastructure();
builder.Services.AddFoundationWebApi();
builder.Services.AddSingleton<ISettingSource>(_ => new InMemorySettingSource(
[
    new SettingEntry(
        SettingScope.Global,
        WorkbenchPlatformReference.DefaultCultureSetting,
        "ar-YE"),
    new SettingEntry(
        SettingScope.Global,
        WorkbenchPlatformReference.DefaultTimeZoneSetting,
        "UTC"),
    new SettingEntry(
        SettingScope.Global,
        SettingBackedFeatureEvaluator.GetEnabledSettingKey(
            WorkbenchPlatformReference.CatalogPreviewFeature),
        "true")
]));
builder.Services.AddSingleton<ISettingReader, SettingReader>();
builder.Services.AddSingleton<IFeatureEvaluator, SettingBackedFeatureEvaluator>();
builder.Services.AddSingleton(_ => new SupportedCultureSet(
    ["ar-YE", "en-US"],
    "ar-YE"));
builder.Services.AddSingleton<ICacheStore>(_ => new InMemoryCacheStore(
    new InMemoryCacheOptions
    {
        MaximumEntries = 128,
        MaximumValueBytes = 1_048_576,
        MaximumTimeToLive = TimeSpan.FromHours(1)
    }));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FoundationKit Dual Full-Stack API",
        Version = "v1",
        Description = "One shared host exposing a complete user stack, a complete admin stack, and the workflow boundary connecting them."
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
builder.Services.AddScoped<IRepository<AdminReview, Guid>,
    EfRepository<AdminReview, Guid, WorkbenchDbContext>>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork<WorkbenchDbContext>>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<CatalogService>();
builder.Services.AddSingleton<ICapabilityCatalog>(serviceProvider =>
    serviceProvider.GetRequiredService<CatalogService>());
builder.Services.AddScoped<CreateUserRequestUseCase>();
builder.Services.AddScoped<ReviewUserRequestUseCase>();
builder.Services.AddScoped<IAdminQueueReader, EfAdminQueueReader>();
builder.Services.AddScoped<IDomainEventHandler<BuildBriefCreated>, BuildBriefCreatedHandler>();

var app = builder.Build();

app.UseFoundationRequestPipeline();
app.UseCors("LocalWorkbenchClient");
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "FoundationKit Dual Full-Stack API v1");
    options.DocumentTitle = "FoundationKit Dual Full-Stack API";
});
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

await DatabaseBootstrapper.MigrateAsync(
    app.Services,
    app.Logger,
    app.Lifetime.ApplicationStopping);

app.MapSystemEndpoints();
app.MapUserPortalEndpoints();
app.MapAdminPortalEndpoints();

app.MapFallbackToFile("index.html");
app.Run();

public partial class Program
{
}
