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

        ProductionConfigurationValidator.Validate(
            configuration,
            isDevelopment: true);
    }

    [Fact]
    public void Production_rejects_wildcard_allowed_hosts()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AllowedHosts"] = "*"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                configuration,
                isDevelopment: false));

        Assert.Contains("AllowedHosts", exception.Message, StringComparison.Ordinal);
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
            ProductionConfigurationValidator.Validate(
                configuration,
                isDevelopment: false));

        Assert.Contains("AdminSeed", exception.Message, StringComparison.Ordinal);
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
            ProductionConfigurationValidator.Validate(
                configuration,
                isDevelopment: false));

        Assert.Contains("migrations", exception.Message, StringComparison.OrdinalIgnoreCase);
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
            ProductionConfigurationValidator.Validate(
                configuration,
                isDevelopment: false));

        Assert.Contains("role seeding", exception.Message, StringComparison.OrdinalIgnoreCase);
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

    private static IConfiguration BuildConfiguration(
        IReadOnlyDictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
