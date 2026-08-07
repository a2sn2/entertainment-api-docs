using System.Security.Claims;
using Athar.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Athar.Api;

public static class ProductionConfigurationValidator
{
    public static void Validate(
        IConfiguration configuration,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (isDevelopment)
            return;

        var allowedHosts = configuration["AllowedHosts"];
        var hasWildcardHost = string.IsNullOrWhiteSpace(allowedHosts)
            || allowedHosts
                .Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries
                    | StringSplitOptions.TrimEntries)
                .Any(host => host == "*");

        if (hasWildcardHost)
        {
            throw new InvalidOperationException(
                "Production requires an explicit AllowedHosts allow-list; wildcard hosts are not permitted.");
        }

        if (configuration.GetValue<bool>(
                $"{AdminSeedOptions.SectionName}:Enabled"))
        {
            throw new InvalidOperationException(
                "AdminSeed must be disabled outside Development. Use the controlled administrator onboarding process.");
        }

        if (configuration.GetValue<bool>(
                $"{DatabaseStartupOptions.SectionName}:ApplyMigrationsOnStartup"))
        {
            throw new InvalidOperationException(
                "Automatic database migrations are not permitted outside Development. Apply reviewed migrations as a controlled deployment step.");
        }

        if (configuration.GetValue<bool>(
                $"{DatabaseStartupOptions.SectionName}:SeedRolesOnStartup"))
        {
            throw new InvalidOperationException(
                "Automatic role seeding is not permitted outside Development. Provision required roles through the controlled deployment process.");
        }
    }
}

public static class AtharRateLimitPartitions
{
    public static string Authentication(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return RemoteAddress(context);
    }

    public static string Write(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId)
            ? RemoteAddress(context)
            : $"user:{userId}";
    }

    private static string RemoteAddress(HttpContext context) =>
        $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}
