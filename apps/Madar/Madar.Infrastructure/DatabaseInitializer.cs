using Madar.Contracts.Security;
using Madar.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Madar.Infrastructure;

public sealed class MadarBootstrapOptions
{
    public const string SectionName = "Madar:Bootstrap";

    public bool Enabled { get; set; }

    public string? AdministratorEmail { get; set; }

    public string? AdministratorPassword { get; set; }

    public string? AdministratorDisplayName { get; set; }

    public string? OperatorEmail { get; set; }

    public string? OperatorPassword { get; set; }

    public string? OperatorDisplayName { get; set; }
}

public static class DatabaseInitializer
{
    private static readonly string[] Roles =
    [
        MadarRoles.Requester,
        MadarRoles.Operator,
        MadarRoles.Supervisor,
        MadarRoles.Administrator
    ];

    private static readonly Action<ILogger, int, int, Exception?> DatabaseRetry =
        LoggerMessage.Define<int, int>(
            LogLevel.Warning,
            new EventId(4101, nameof(DatabaseRetry)),
            "Madar database startup attempt {Attempt}/{Maximum} failed. Retrying.");

    private static readonly Action<ILogger, int, Exception?> DatabaseReady =
        LoggerMessage.Define<int>(
            LogLevel.Information,
            new EventId(4102, nameof(DatabaseReady)),
            "Madar database startup completed on attempt {Attempt}.");

    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var scoped = scope.ServiceProvider;
        var dbContext = scoped.GetRequiredService<MadarDbContext>();
        var startup = scoped
            .GetRequiredService<IOptions<MadarDatabaseStartupOptions>>()
            .Value;
        var logger = scoped.GetRequiredService<ILogger<MadarDbContext>>();

        await DatabaseStartupRetry.ExecuteAsync(
            async (attempt, token) =>
            {
                if (startup.ApplyMigrationsOnStartup)
                {
                    await dbContext.Database.MigrateAsync(token);
                }
                else
                {
                    if (!await dbContext.Database.CanConnectAsync(token))
                    {
                        throw new InvalidOperationException(
                            "Madar database is not reachable.");
                    }

                    var pending = await dbContext.Database
                        .GetPendingMigrationsAsync(token);
                    if (pending.Any())
                    {
                        throw new InvalidOperationException(
                            "Madar database has pending migrations while startup migration application is disabled.");
                    }
                }

                DatabaseReady(logger, attempt, null);
            },
            startup.MigrationAttempts,
            TimeSpan.FromSeconds(startup.DelaySeconds),
            (attempt, maximum, exception) =>
                DatabaseRetry(logger, attempt, maximum, exception),
            cancellationToken);

        if (startup.SeedRolesOnStartup)
        {
            var roleManager = scoped
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            foreach (var role in Roles)
                await EnsureRoleAsync(roleManager, role);
        }

        var options = scoped
            .GetRequiredService<IOptions<MadarBootstrapOptions>>()
            .Value;
        if (!options.Enabled)
            return;

        var userManager = scoped.GetRequiredService<UserManager<MadarUser>>();
        await EnsureUserAsync(
            userManager,
            options.AdministratorEmail!,
            options.AdministratorPassword!,
            options.AdministratorDisplayName!,
            MadarRoles.Administrator,
            cancellationToken);
        await EnsureUserAsync(
            userManager,
            options.OperatorEmail!,
            options.OperatorPassword!,
            options.OperatorDisplayName!,
            MadarRoles.Operator,
            cancellationToken);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        string role)
    {
        if (await roleManager.RoleExistsAsync(role))
            return;

        var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        EnsureSucceeded(result, $"create role '{role}'");
    }

    private static async Task EnsureUserAsync(
        UserManager<MadarUser> userManager,
        string email,
        string password,
        string displayName,
        string role,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedEmail = email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            user = new MadarUser
            {
                Id = Guid.NewGuid(),
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = true,
                DisplayName = displayName.Trim(),
                CreatedUtc = DateTimeOffset.UtcNow
            };

            var create = await userManager.CreateAsync(user, password);
            EnsureSucceeded(create, $"create bootstrap user '{normalizedEmail}'");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var addRole = await userManager.AddToRoleAsync(user, role);
            EnsureSucceeded(addRole, $"assign bootstrap role '{role}'");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
            return;

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException(
            $"Madar database initialization failed during {operation}: {errors}");
    }
}
