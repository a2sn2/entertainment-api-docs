using System.Data.Common;
using System.Net;
using System.Security.Claims;
using Athar.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Athar.Api;

public sealed class ReverseProxySecurityOptions
{
    public const string SectionName = "ReverseProxy";

    public bool Enabled { get; set; }

    public string[] KnownProxies { get; set; } = [];
}

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
        ValidateExplicitSecurityDecisions(configuration);
        ValidateReverseProxy(configuration);
        ValidateNoStartupPrivilege(configuration);
        ValidateAccountRecoveryDelivery(configuration);
        ValidateDataProtection(configuration);
        ValidateDatabaseTransportAndIdentity(
            configuration.GetConnectionString("Athar"));
    }

    private static void ValidateAllowedHosts(IConfiguration configuration)
    {
        var allowedHosts = configuration["AllowedHosts"];
        var hasWildcardHost = string.IsNullOrWhiteSpace(allowedHosts)
            || allowedHosts.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(host => host == "*");

        if (hasWildcardHost)
            throw new InvalidOperationException("Production requires an explicit AllowedHosts allow-list; wildcard hosts are not permitted.");
    }

    private static void ValidateExplicitSecurityDecisions(IConfiguration configuration)
    {
        RequireExplicitBooleanDecision(
            configuration,
            $"{AccountSecurityOptions.SectionName}:RequireConfirmedEmail");
        RequireExplicitBooleanDecision(
            configuration,
            $"{AccountSecurityOptions.SectionName}:RequireAdministratorMfa");
        RequireExplicitIntegerDecision(
            configuration,
            $"{AccountSecurityOptions.SectionName}:PasswordRequiredLength",
            minimum: 1,
            maximum: 128);
        RequireExplicitBooleanDecision(
            configuration,
            $"{AccountSecurityOptions.SectionName}:PasswordRequireDigit");
        RequireExplicitBooleanDecision(
            configuration,
            $"{AccountSecurityOptions.SectionName}:PasswordRequireLowercase");
        RequireExplicitBooleanDecision(
            configuration,
            $"{AccountSecurityOptions.SectionName}:PasswordRequireUppercase");
        RequireExplicitBooleanDecision(
            configuration,
            $"{AccountSecurityOptions.SectionName}:PasswordRequireNonAlphanumeric");
        RequireExplicitBooleanDecision(
            configuration,
            $"{ReverseProxySecurityOptions.SectionName}:Enabled");
    }

    private static void RequireExplicitBooleanDecision(
        IConfiguration configuration,
        string key)
    {
        var raw = configuration[key];
        if (string.IsNullOrWhiteSpace(raw) || !bool.TryParse(raw, out _))
            throw new InvalidOperationException($"Production requires an explicit true/false decision for '{key}'.");
    }

    private static void RequireExplicitIntegerDecision(
        IConfiguration configuration,
        string key,
        int minimum,
        int maximum)
    {
        var raw = configuration[key];
        if (!int.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value)
            || value < minimum
            || value > maximum)
        {
            throw new InvalidOperationException(
                $"Production requires an explicit integer decision for '{key}' in the supported range {minimum}..{maximum}.");
        }
    }

    private static void ValidateReverseProxy(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>($"{ReverseProxySecurityOptions.SectionName}:Enabled"))
            return;

        var proxies = configuration
            .GetSection($"{ReverseProxySecurityOptions.SectionName}:KnownProxies")
            .Get<string[]>()
            ?? [];

        if (proxies.Length == 0)
        {
            throw new InvalidOperationException(
                "ReverseProxy:Enabled=true requires at least one explicit trusted proxy IP in ReverseProxy:KnownProxies. Trust-all forwarded headers are not permitted.");
        }

        foreach (var proxy in proxies)
        {
            if (!IPAddress.TryParse(proxy, out _))
            {
                throw new InvalidOperationException(
                    $"ReverseProxy:KnownProxies contains an invalid IP address: '{proxy}'.");
            }
        }
    }

    private static void ValidateNoStartupPrivilege(IConfiguration configuration)
    {
        if (configuration.GetValue<bool>($"{AdminSeedOptions.SectionName}:Enabled"))
            throw new InvalidOperationException("AdminSeed must be disabled outside Development. Use the controlled administrator onboarding process.");

        if (configuration.GetValue<bool>($"{DatabaseStartupOptions.SectionName}:ApplyMigrationsOnStartup"))
            throw new InvalidOperationException("Automatic database migrations are not permitted outside Development. Apply reviewed migrations as a controlled deployment step.");

        if (configuration.GetValue<bool>($"{DatabaseStartupOptions.SectionName}:SeedRolesOnStartup"))
            throw new InvalidOperationException("Automatic role seeding is not permitted outside Development. Provision required roles through the controlled deployment process.");
    }

    private static void ValidateAccountRecoveryDelivery(IConfiguration configuration)
    {
        var host = configuration[$"{AccountSecurityOptions.SectionName}:SmtpHost"];
        var fromAddress = configuration[$"{AccountSecurityOptions.SectionName}:FromAddress"];
        var port = configuration.GetValue<int?>($"{AccountSecurityOptions.SectionName}:SmtpPort");
        var tlsEnabled = configuration.GetValue<bool?>($"{AccountSecurityOptions.SectionName}:SmtpEnableSsl");

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress) || port is null or < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "Production requires operational SMTP account-notification configuration so email confirmation and password recovery tokens are not exposed through logs or API responses.");
        }

        if (tlsEnabled is not true)
        {
            throw new InvalidOperationException(
                "Production account notifications require SMTP transport security. AccountSecurity:SmtpEnableSsl must be explicitly true unless a separately approved secure relay implementation replaces this sender.");
        }
    }

    private static void ValidateDataProtection(IConfiguration configuration)
    {
        var keysPath = configuration["DataProtection:KeysPath"];
        var certificatePath = configuration["DataProtection:CertificatePath"];

        if (string.IsNullOrWhiteSpace(keysPath))
            throw new InvalidOperationException("Production requires a durable access-controlled Data Protection key path.");

        if (string.IsNullOrWhiteSpace(certificatePath))
            throw new InvalidOperationException("Production requires an X.509 certificate to protect persisted Data Protection keys at rest.");
    }

    private static void ValidateDatabaseTransportAndIdentity(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Production requires the Athar connection string to be supplied by the deployment environment or secret manager.");

        var parsed = new DbConnectionStringBuilder { ConnectionString = connectionString };
        var encrypt = GetValue(parsed, "Encrypt");
        if (encrypt is null || (encrypt.Equals("true", StringComparison.OrdinalIgnoreCase) is false
            && encrypt.Equals("mandatory", StringComparison.OrdinalIgnoreCase) is false
            && encrypt.Equals("strict", StringComparison.OrdinalIgnoreCase) is false))
            throw new InvalidOperationException("Production SQL Server connections must enable transport encryption (Encrypt=True/Mandatory/Strict).");

        var trustServerCertificate = GetValue(parsed, "TrustServerCertificate", "Trust Server Certificate");
        if (bool.TryParse(trustServerCertificate, out var trustsAnyCertificate) && trustsAnyCertificate)
            throw new InvalidOperationException("Production SQL Server connections must validate the server certificate; TrustServerCertificate=True is not permitted.");

        var userId = GetValue(parsed, "User Id", "UserID", "UID");
        if (string.Equals(userId, "sa", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production runtime must not use the SQL Server sa account. Provision a least-privilege application principal and a separate migration principal.");
    }

    private static string? GetValue(DbConnectionStringBuilder parsed, params string[] keys)
    {
        foreach (var key in parsed.Keys.Cast<string>())
        {
            if (!keys.Any(candidate => candidate.Equals(key, StringComparison.OrdinalIgnoreCase)))
                continue;
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
        return string.IsNullOrWhiteSpace(userId) ? RemoteAddress(context) : $"user:{userId}";
    }

    private static string RemoteAddress(HttpContext context) =>
        $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}
