using System.Net;
using System.Security.Claims;
using Athar.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, isDevelopment: false));
        Assert.True(exception.Message.Contains("AllowedHosts", StringComparison.Ordinal));
    }

    [Fact]
    public void Production_rejects_admin_seed()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "athar.example.invalid",
            ["AdminSeed:Enabled"] = "true"
        });
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, isDevelopment: false));
        Assert.True(exception.Message.Contains("AdminSeed", StringComparison.Ordinal));
    }

    [Fact]
    public void Production_rejects_automatic_migrations()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "athar.example.invalid",
            ["DatabaseStartup:ApplyMigrationsOnStartup"] = "true"
        });
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, isDevelopment: false));
        Assert.True(exception.Message.Contains("migrations", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Production_rejects_automatic_role_seed()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "athar.example.invalid",
            ["DatabaseStartup:SeedRolesOnStartup"] = "true"
        });
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, isDevelopment: false));
        Assert.True(exception.Message.Contains("role seeding", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Production_rejects_missing_account_recovery_delivery()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "athar.example.invalid",
            ["ConnectionStrings:Athar"] = SecureConnectionString
        });
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, isDevelopment: false));
        Assert.True(exception.Message.Contains("SMTP", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Production_rejects_unencrypted_database_transport()
    {
        var configuration = ProductionConfiguration(
            "Server=db.internal;Database=Athar;User Id=athar_app;Password=${ATHAR_DB_PASSWORD};Encrypt=False;TrustServerCertificate=True");
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, isDevelopment: false));
        Assert.True(exception.Message.Contains("encryption", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Production_rejects_sa_runtime_identity()
    {
        var configuration = ProductionConfiguration(
            "Server=db.internal;Database=Athar;User Id=sa;Password=${ATHAR_DB_PASSWORD};Encrypt=True;TrustServerCertificate=False");
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(configuration, isDevelopment: false));
        Assert.True(exception.Message.Contains("sa account", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Production_accepts_explicit_hosts_encrypted_database_and_no_startup_privilege()
    {
        var configuration = ProductionConfiguration(SecureConnectionString);
        ProductionConfigurationValidator.Validate(configuration, isDevelopment: false);
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
        Assert.NotEqual(
            AtharRateLimitPartitions.Authentication(first),
            AtharRateLimitPartitions.Authentication(second));
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

    private const string SecureConnectionString =
        "Server=db.internal;Database=Athar;User Id=athar_app;Password=${ATHAR_DB_PASSWORD};Encrypt=True;TrustServerCertificate=False";

    private static IConfiguration ProductionConfiguration(string connectionString) =>
        BuildConfiguration(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "athar.example.invalid",
            ["AdminSeed:Enabled"] = "false",
            ["DatabaseStartup:ApplyMigrationsOnStartup"] = "false",
            ["DatabaseStartup:SeedRolesOnStartup"] = "false",
            ["AccountSecurity:SmtpHost"] = "smtp.example.invalid",
            ["AccountSecurity:SmtpPort"] = "587",
            ["AccountSecurity:FromAddress"] = "security@example.invalid",
            ["ConnectionStrings:Athar"] = connectionString
        });

    private static IConfiguration BuildConfiguration(
        IReadOnlyDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
