using System.Net;
using System.Security.Claims;
using Athar.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Athar.Tests;

public sealed class SecurityConfigurationTests
{
    [Fact]
    public void Development_allows_local_convenience_settings()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "*",
            ["AdminSeed:Enabled"] = "true",
            ["DatabaseStartup:ApplyMigrationsOnStartup"] = "true",
            ["DatabaseStartup:SeedRolesOnStartup"] = "true"
        });
        ProductionConfigurationValidator.Validate(configuration, isDevelopment: true);
    }

    [Fact]
    public void Production_rejects_wildcard_allowed_hosts()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?> { ["AllowedHosts"] = "*" });
        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationValidator.Validate(configuration, false));
        Assert.Contains("AllowedHosts", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AccountSecurity:RequireConfirmedEmail")]
    [InlineData("AccountSecurity:RequireAdministratorMfa")]
    [InlineData("ReverseProxy:Enabled")]
    public void Production_rejects_missing_explicit_security_decision(string key)
    {
        var values = BaseProductionValues(SecureConnectionString);
        values.Remove(key);
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), false));
        Assert.Contains(key, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_rejects_enabled_reverse_proxy_without_trusted_proxy()
    {
        var values = BaseProductionValues(SecureConnectionString);
        values["ReverseProxy:Enabled"] = "true";
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), false));
        Assert.Contains("KnownProxies", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_rejects_invalid_trusted_proxy_ip()
    {
        var values = BaseProductionValues(SecureConnectionString);
        values["ReverseProxy:Enabled"] = "true";
        values["ReverseProxy:KnownProxies:0"] = "not-an-ip";
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), false));
        Assert.Contains("invalid IP", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_rejects_admin_seed()
    {
        var values = BaseProductionValues(SecureConnectionString);
        values["AdminSeed:Enabled"] = "true";
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), false));
        Assert.Contains("AdminSeed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_rejects_automatic_migrations()
    {
        var values = BaseProductionValues(SecureConnectionString);
        values["DatabaseStartup:ApplyMigrationsOnStartup"] = "true";
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), false));
        Assert.Contains("migrations", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_rejects_automatic_role_seed()
    {
        var values = BaseProductionValues(SecureConnectionString);
        values["DatabaseStartup:SeedRolesOnStartup"] = "true";
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), false));
        Assert.Contains("role seeding", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_rejects_missing_account_recovery_delivery()
    {
        var values = BaseProductionValues(SecureConnectionString);
        values.Remove("AccountSecurity:SmtpHost");
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), false));
        Assert.Contains("SMTP", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_rejects_smtp_without_transport_security()
    {
        var values = BaseProductionValues(SecureConnectionString);
        values["AccountSecurity:SmtpEnableSsl"] = "false";
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), false));
        Assert.Contains("transport security", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_rejects_missing_Data_Protection_key_material()
    {
        var values = BaseProductionValues(SecureConnectionString);
        values.Remove("DataProtection:KeysPath");
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(BuildConfiguration(values), false));
        Assert.Contains("Data Protection", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_rejects_unencrypted_database_transport()
    {
        var configuration = ProductionConfiguration(
            "Server=db.internal;Database=Athar;User Id=athar_app;Password=${ATHAR_DB_PASSWORD};Encrypt=False;TrustServerCertificate=True");
        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationValidator.Validate(configuration, false));
        Assert.Contains("encryption", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_rejects_sa_runtime_identity()
    {
        var configuration = ProductionConfiguration(
            "Server=db.internal;Database=Athar;User Id=sa;Password=${ATHAR_DB_PASSWORD};Encrypt=True;TrustServerCertificate=False");
        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationValidator.Validate(configuration, false));
        Assert.Contains("sa account", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Production_accepts_explicit_false_security_decisions_and_secure_transport()
    {
        ProductionConfigurationValidator.Validate(ProductionConfiguration(SecureConnectionString), false);
    }

    [Fact]
    public void Production_accepts_reverse_proxy_with_explicit_trusted_ip()
    {
        var values = BaseProductionValues(SecureConnectionString);
        values["ReverseProxy:Enabled"] = "true";
        values["ReverseProxy:KnownProxies:0"] = "10.0.0.2";
        ProductionConfigurationValidator.Validate(BuildConfiguration(values), false);
    }

    [Fact]
    public void Authentication_rate_limit_partition_is_per_remote_ip()
    {
        var first = new DefaultHttpContext();
        first.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
        var second = new DefaultHttpContext();
        second.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.11");
        Assert.Equal("ip:192.0.2.10", AtharRateLimitPartitions.Authentication(first));
        Assert.Equal("ip:192.0.2.11", AtharRateLimitPartitions.Authentication(second));
        Assert.NotEqual(AtharRateLimitPartitions.Authentication(first), AtharRateLimitPartitions.Authentication(second));
    }

    [Fact]
    public void Authenticated_write_rate_limit_partition_is_per_user()
    {
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                authenticationType: "test"))
        };
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
        Assert.Equal($"user:{userId}", AtharRateLimitPartitions.Write(context));
    }

    [Fact]
    public async Task Trusted_forwarded_headers_replace_proxy_ip_and_original_scheme_before_security_decisions()
    {
        var trustedProxy = IPAddress.Parse("10.0.0.2");
        var client = IPAddress.Parse("203.0.113.42");
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = trustedProxy;
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = client.ToString();
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await ApplyForwardedHeadersAsync(context, trustedProxy);

        Assert.Equal(client, context.Connection.RemoteIpAddress);
        Assert.Equal("https", context.Request.Scheme);
        Assert.Equal($"ip:{client}", AtharRateLimitPartitions.Authentication(context));
    }

    [Fact]
    public async Task Untrusted_direct_forwarded_headers_are_ignored()
    {
        var trustedProxy = IPAddress.Parse("10.0.0.2");
        var directClient = IPAddress.Parse("198.51.100.77");
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = directClient;
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.42";
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await ApplyForwardedHeadersAsync(context, trustedProxy);

        Assert.Equal(directClient, context.Connection.RemoteIpAddress);
        Assert.Equal("http", context.Request.Scheme);
        Assert.Equal($"ip:{directClient}", AtharRateLimitPartitions.Authentication(context));
    }

    private const string SecureConnectionString =
        "Server=db.internal;Database=Athar;User Id=athar_app;Password=${ATHAR_DB_PASSWORD};Encrypt=True;TrustServerCertificate=False";

    private static IConfiguration ProductionConfiguration(string connectionString) =>
        BuildConfiguration(BaseProductionValues(connectionString));

    private static Dictionary<string, string?> BaseProductionValues(string connectionString) =>
        new()
        {
            ["AllowedHosts"] = "athar.example.invalid",
            ["AdminSeed:Enabled"] = "false",
            ["DatabaseStartup:ApplyMigrationsOnStartup"] = "false",
            ["DatabaseStartup:SeedRolesOnStartup"] = "false",
            ["AccountSecurity:RequireConfirmedEmail"] = "false",
            ["AccountSecurity:RequireAdministratorMfa"] = "false",
            ["AccountSecurity:SmtpHost"] = "smtp.example.invalid",
            ["AccountSecurity:SmtpPort"] = "587",
            ["AccountSecurity:SmtpEnableSsl"] = "true",
            ["AccountSecurity:FromAddress"] = "security@example.invalid",
            ["ReverseProxy:Enabled"] = "false",
            ["DataProtection:KeysPath"] = "/var/athar/dpkeys",
            ["DataProtection:CertificatePath"] = "/run/secrets/athar-dp.pfx",
            ["ConnectionStrings:Athar"] = connectionString
        };

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static async Task ApplyForwardedHeadersAsync(
        HttpContext context,
        IPAddress trustedProxy)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownProxies.Add(trustedProxy);

        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(options));

        await middleware.Invoke(context);
    }
}
