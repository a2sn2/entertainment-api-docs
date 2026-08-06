using FoundationKit.Application.Results;
using FoundationKit.Blazor.Api;
using FoundationKit.Domain.Primitives;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.WebApi.Results;

namespace FoundationKit.Tests;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void Domain_has_no_outer_layer_or_framework_dependencies()
    {
        AssertNoReferences(
            typeof(Entity<>).Assembly,
            "FoundationKit.Application",
            "FoundationKit.Infrastructure",
            "FoundationKit.WebApi",
            "FoundationKit.Blazor",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Application_depends_only_on_domain_and_base_framework()
    {
        AssertNoReferences(
            typeof(Result).Assembly,
            "FoundationKit.Infrastructure",
            "FoundationKit.WebApi",
            "FoundationKit.Blazor",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Infrastructure_does_not_select_a_database_provider_or_web_host()
    {
        AssertNoReferences(
            typeof(EfRepository<,,>).Assembly,
            "Microsoft.EntityFrameworkCore.SqlServer",
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            "Microsoft.EntityFrameworkCore.Sqlite",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void WebApi_does_not_depend_on_infrastructure_or_blazor()
    {
        AssertNoReferences(
            typeof(ResultHttpExtensions).Assembly,
            "FoundationKit.Infrastructure",
            "FoundationKit.Blazor",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Blazor_does_not_depend_on_server_layers()
    {
        AssertNoReferences(
            typeof(ApiResult).Assembly,
            "FoundationKit.Domain",
            "FoundationKit.Application",
            "FoundationKit.Infrastructure",
            "FoundationKit.WebApi",
            "Microsoft.EntityFrameworkCore");
    }

    private static void AssertNoReferences(
        System.Reflection.Assembly assembly,
        params string[] forbidden)
    {
        var references = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        foreach (var forbiddenName in forbidden)
        {
            Assert.DoesNotContain(
                references,
                reference => reference.Equals(
                    forbiddenName,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
