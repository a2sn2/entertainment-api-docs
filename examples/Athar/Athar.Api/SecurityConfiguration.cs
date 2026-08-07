using System.Data.Common;
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

        ValidateAllowedHosts(configuration);
        ValidateNoStartupPrivilege(configuration);
        ValidateAccountRecoveryDelivery(configuration);
        ValidateDatabaseTransportAndIdentity(
            configuration.GetConnectionString("Athar"));
    }

    private static void ValidateAllowedHosts(IConfiguration configuration)
    {
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
    }

    private static void ValidateNoStartupPrivilege(IConfiguration configuration)
    {
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

    private static void ValidateAccountRecoveryDelivery(
        IConfiguration configuration)
    {
        var host = configuration[$"{AccountSecurityOptions.SectionName}:SmtpHost"];
        var fromAddress = configuration[$"{AccountSecurityOptions.SectionName}:FromAddress"];
        var port = configuration.GetValue<int?>(
            $"{AccountSecurityOptions.SectionName}:SmtpPort");

        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(fromAddress)
            || port is null or < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "Production requires operational SMTP account-notification configuration so email confirmation and password recovery tokens are not exposed through logs or API responses.");
        }
    }

    private static void ValidateDatabaseTransportAndIdentity(
        string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Production requires the Athar connection string to be supplied by the deployment environment or secret manager.");
        }

        var parsed = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        var encrypt = GetValue(parsed, "Encrypt");
        if (encrypt is null
            || (encrypt.Equals("true", StringComparison.OrdinalIgnoreCase) is false
                && encrypt.Equals("mandatory", StringComparison.OrdinalIgnoreCase) is false
                && encrypt.Equals("strict", StringComparison.OrdinalIgnoreCase) is false))
        {
            throw new InvalidOperationException(
                "Production SQL Server connections must enable transport encryption (Encrypt=True/Mandatory/Strict)."
            );
        }

        var trustServerCertificate = GetValue(
            parsed,
            "TrustServerCertificate",
            "Trust Server Certificate");
        if (bool.TryParse(trustServerCertificate, out var trustsAnyCertificate)
            && trustsAnyCertificate)
        {
            throw new InvalidOperationException(
                "Production SQL Server connections must validate the server certificate; TrustServerCertificate=True is not permitted."
            );
        }

        var userId = GetValue(parsed, "User Id", "UserID", "UID");
        if (string.Equals(userId, "sa", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Production runtime must not use the SQL Server sa account. Provision a least-privilege application principal and a separate migration principal."
            );
        }
    }

    private static string? GetValue(
        DbConnectionStringBuilder parsed,
        params string[] keys)
    {
        foreach (var key in parsed.Keys.Cast<string>())
        {
            if (!keys.Any(candidate =>
                    candidate.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            return Convert.ToString(parsed[key], System.Globalization.CultureInfo.InvariantCulture);
        }

        return null;
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
