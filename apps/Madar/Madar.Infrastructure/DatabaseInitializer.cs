using Madar.Contracts.Security;
using Madar.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MadarDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles)
            await EnsureRoleAsync(roleManager, role);

        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<MadarBootstrapOptions>>()
            .Value;
        if (!options.Enabled)
            return;

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<MadarUser>>();
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
