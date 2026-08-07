using Athar.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Athar.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<AtharDbContext>>();
        var startup = services
            .GetRequiredService<IOptions<DatabaseStartupOptions>>()
            .Value;

        if (startup.ApplyMigrationsOnStartup)
        {
            await MigrateWithRetryAsync(
                services.GetRequiredService<AtharDbContext>(),
                logger,
                startup,
                cancellationToken);
        }
        else
        {
            await ValidateSchemaWithRetryAsync(
                services.GetRequiredService<AtharDbContext>(),
                logger,
                startup,
                cancellationToken);
        }

        if (startup.SeedRolesOnStartup)
            await SeedRolesAsync(services, cancellationToken);

        await SeedAdministratorAsync(
            services,
            logger,
            startup,
            cancellationToken);
    }

    private static async Task MigrateWithRetryAsync(
        AtharDbContext dbContext,
        ILogger logger,
        DatabaseStartupOptions options,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= options.MigrationAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation(
                    "Athar database migrations completed on attempt {Attempt}.",
                    attempt);
                return;
            }
            catch (Exception exception)
                when (attempt < options.MigrationAttempts)
            {
                lastError = exception;
                logger.LogWarning(
                    exception,
                    "Athar database migration attempt {Attempt}/{Maximum} failed. Retrying.",
                    attempt,
                    options.MigrationAttempts);

                await Task.Delay(
                    TimeSpan.FromSeconds(options.DelaySeconds),
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Athar database migrations failed after the configured retries.",
            lastError);
    }

    private static async Task ValidateSchemaWithRetryAsync(
        AtharDbContext dbContext,
        ILogger logger,
        DatabaseStartupOptions options,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= options.MigrationAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (!await dbContext.Database.CanConnectAsync(cancellationToken))
                    throw new InvalidOperationException("Athar database is not reachable.");

                var pending = (await dbContext.Database
                        .GetPendingMigrationsAsync(cancellationToken))
                    .ToArray();

                if (pending.Length > 0)
                {
                    throw new InvalidOperationException(
                        "Athar database has pending migrations. Apply reviewed migrations through the controlled deployment process before starting the application: "
                        + string.Join(", ", pending));
                }

                logger.LogInformation(
                    "Athar database schema validation completed on attempt {Attempt}; no pending migrations were found.",
                    attempt);
                return;
            }
            catch (Exception exception)
                when (attempt < options.MigrationAttempts)
            {
                lastError = exception;
                logger.LogWarning(
                    exception,
                    "Athar database schema validation attempt {Attempt}/{Maximum} failed. Retrying.",
                    attempt,
                    options.MigrationAttempts);

                await Task.Delay(
                    TimeSpan.FromSeconds(options.DelaySeconds),
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Athar database schema validation failed after the configured retries.",
            lastError);
    }

    private static async Task SeedRolesAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in new[] { AtharRoles.User, AtharRoles.Administrator })
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var result = await roleManager.CreateAsync(
                new IdentityRole<Guid>(roleName));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"تعذر إنشاء الدور {roleName}: "
                    + string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }
    }

    private static async Task SeedAdministratorAsync(
        IServiceProvider services,
        ILogger logger,
        DatabaseStartupOptions startup,
        CancellationToken cancellationToken)
    {
        var options = services
            .GetRequiredService<IOptions<AdminSeedOptions>>()
            .Value;

        if (!options.Enabled)
        {
            logger.LogInformation(
                "Admin seeding is disabled. Create the production administrator through the controlled onboarding process.");
            return;
        }

        if (!startup.SeedRolesOnStartup)
        {
            throw new InvalidOperationException(
                "AdminSeed requires explicit development role seeding. Enable DatabaseStartup:SeedRolesOnStartup only in a controlled development/test environment.");
        }

        if (string.IsNullOrWhiteSpace(options.Email)
            || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException(
                "AdminSeed is enabled but Email or Password is missing.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var userManager = services.GetRequiredService<UserManager<AtharUser>>();
        var existing = await userManager.FindByEmailAsync(options.Email);

        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, AtharRoles.Administrator))
            {
                throw new InvalidOperationException(
                    "AdminSeed refuses to promote an existing non-administrator account. Use the controlled administrator onboarding process instead.");
            }

            return;
        }

        var user = new AtharUser
        {
            Id = Guid.NewGuid(),
            Email = options.Email.Trim(),
            UserName = options.Email.Trim(),
            DisplayName = options.DisplayName.Trim(),
            EmailConfirmed = true,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        var create = await userManager.CreateAsync(user, options.Password);
        EnsureIdentitySucceeded(create, "إنشاء حساب المسؤول");

        var role = await userManager.AddToRoleAsync(
            user,
            AtharRoles.Administrator);
        EnsureIdentitySucceeded(role, "إضافة دور المسؤول");
    }

    private static void EnsureIdentitySucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
            return;

        throw new InvalidOperationException(
            $"{operation} فشلت: "
            + string.Join("; ", result.Errors.Select(error => error.Description)));
    }
}
