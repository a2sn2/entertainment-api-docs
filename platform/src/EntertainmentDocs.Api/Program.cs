using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text;
using EntertainmentDocs.Api.Authorization;
using EntertainmentDocs.Api.Endpoints;
using EntertainmentDocs.Api.Services;
using EntertainmentDocs.Application;
using EntertainmentDocs.Application.Abstractions;
using EntertainmentDocs.Infrastructure;
using EntertainmentDocs.Infrastructure.Identity;
using EntertainmentDocs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddPolicy("WebClients", policy =>
{
    var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length == 0)
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    else
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 32 characters.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.ManageContent, policy => policy.RequireRole(SystemRoles.Administrator, SystemRoles.Editor));
    options.AddPolicy(Policies.PublishContent, policy => policy.RequireRole(SystemRoles.Administrator, SystemRoles.Reviewer));
    options.AddPolicy(Policies.ManageUsers, policy => policy.RequireRole(SystemRoles.Administrator));
});

builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("api", limiter =>
{
    limiter.PermitLimit = 120;
    limiter.Window = TimeSpan.FromMinutes(1);
    limiter.QueueLimit = 0;
}));

var app = builder.Build();
app.UseExceptionHandler();
if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();
app.UseCors("WebClients");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "EntertainmentDocs.Api",
    environment = app.Environment.EnvironmentName,
    databaseProvider = "Microsoft SQL Server",
    status = "running"
}));
app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapDocumentEndpoints();
app.MapAdminDocumentEndpoints();
app.MapAdminUserEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true);

    if (app.Environment.IsEnvironment("Testing"))
    {
        await db.Database.EnsureCreatedAsync();
    }
    else if (applyMigrations)
    {
        await db.Database.MigrateAsync();
    }
}

await IdentitySeeder.SeedAsync(app.Services, app.Configuration);
app.Run();

public partial class Program;
