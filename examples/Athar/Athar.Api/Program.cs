using System.Threading.RateLimiting;
using Athar.Api;
using Athar.Application;
using Athar.Contracts;
using Athar.Domain;
using Athar.Infrastructure;
using FoundationKit.Application.Persistence;
using FoundationKit.Infrastructure;
using FoundationKit.Infrastructure.Events;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.WebApi;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

ProductionConfigurationValidator.Validate(
    builder.Configuration,
    builder.Environment.IsDevelopment());

builder.Services.AddFoundationInfrastructure();
builder.Services.AddFoundationWebApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Athar.Application.ICurrentUser, CurrentUserAccessor>();
builder.Services.AddScoped<IInitiativeManager, InitiativeManager>();
builder.Services.AddScoped<IInitiativeQueryService, InitiativeQueryService>();
builder.Services.AddScoped<IAuditWriter, AuditWriter>();
builder.Services.AddScoped<IRepository<Initiative, Guid>,
    EfRepository<Initiative, Guid, AtharDbContext>>();
builder.Services.AddScoped<IRepository<InitiativeReview, Guid>,
    EfRepository<InitiativeReview, Guid, AtharDbContext>>();
builder.Services.AddScoped<FoundationKit.Application.Abstractions.IUnitOfWork,
    EfUnitOfWork<AtharDbContext>>();
builder.Services.AddSingleton<FoundationKit.Application.Abstractions.IClock, SystemClock>();

var connectionString = builder.Configuration.GetConnectionString("Athar")
    ?? throw new InvalidOperationException(
        "Connection string 'Athar' is required.");

builder.Services.AddDbContext<AtharDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(
        connectionString,
        sqlServer => sqlServer.MigrationsAssembly(
            typeof(AtharDbContext).Assembly.FullName));
    options.AddInterceptors(
        serviceProvider.GetRequiredService<DomainEventsSaveChangesInterceptor>());
});

builder.Services
    .AddIdentity<AtharUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AtharDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Athar.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AtharUser", policy =>
        policy.RequireAuthenticatedUser());
    options.AddPolicy("AtharAdministrator", policy =>
        policy.RequireRole(AtharRoles.Administrator));
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "Athar.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: AtharRateLimitPartitions.Authentication(context),
            factory: static _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("write", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: AtharRateLimitPartitions.Write(context),
            factory: static _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services
    .AddOptions<DatabaseStartupOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseStartupOptions.SectionName))
    .Validate(
        options => options.MigrationAttempts is >= 1 and <= 300
            && options.DelaySeconds is >= 1 and <= 30,
        "DatabaseStartup values are outside the supported range.")
    .ValidateOnStart();

builder.Services
    .AddOptions<AdminSeedOptions>()
    .Bind(builder.Configuration.GetSection(AdminSeedOptions.SectionName))
    .Validate(
        options => !options.Enabled
            || (!string.IsNullOrWhiteSpace(options.Email)
                && !string.IsNullOrWhiteSpace(options.Password)
                && options.Password.Length >= 12),
        "When AdminSeed is enabled, Email and a password of at least 12 characters are required.")
    .ValidateOnStart();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "منصة أثر API",
        Version = "v1",
        Description = "مرجع إنتاجي عربي مبني على FoundationKit لإدارة المبادرات المجتمعية."
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseMiddleware<DatabaseExceptionMiddleware>();
app.UseFoundationRequestPipeline();
app.UseRateLimiter();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "منصة أثر API v1");
        options.DocumentTitle = "منصة أثر — Swagger";
    });
}

await DatabaseInitializer.InitializeAsync(
    app.Services,
    app.Lifetime.ApplicationStopping);

app.MapAtharEndpoints();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program
{
}
