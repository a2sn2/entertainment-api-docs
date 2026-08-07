using FoundationKit.Application.Results;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using FoundationKit.Blazor.Api;
using FoundationKit.Domain.Primitives;
using FoundationKit.Identity;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Security;
using FoundationKit.WebApi.Results;
using FoundationKit.Workflow;

namespace FoundationKit.Tests;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void Domain_has_no_outer_layer_framework_or_capability_dependencies()
    {
        AssertNoReferences(
            typeof(Entity<>).Assembly,
            "FoundationKit.Application",
            "FoundationKit.Infrastructure",
            "FoundationKit.WebApi",
            "FoundationKit.Blazor",
            "FoundationKit.Auditing",
            "FoundationKit.Security",
            "FoundationKit.Identity",
            "FoundationKit.Authorization",
            "FoundationKit.Workflow",
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
            "FoundationKit.Auditing",
            "FoundationKit.Security",
            "FoundationKit.Identity",
            "FoundationKit.Authorization",
            "FoundationKit.Workflow",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Infrastructure_does_not_select_a_database_provider_web_host_or_upper_capability()
    {
        AssertNoReferences(
            typeof(EfRepository<,,>).Assembly,
            "FoundationKit.WebApi",
            "FoundationKit.Blazor",
            "FoundationKit.Auditing",
            "FoundationKit.Security",
            "FoundationKit.Identity",
            "FoundationKit.Authorization",
            "FoundationKit.Workflow",
            "Microsoft.EntityFrameworkCore.SqlServer",
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            "Microsoft.EntityFrameworkCore.Sqlite",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void WebApi_does_not_depend_on_infrastructure_blazor_or_upper_capabilities()
    {
        AssertNoReferences(
            typeof(ResultHttpExtensions).Assembly,
            "FoundationKit.Infrastructure",
            "FoundationKit.Blazor",
            "FoundationKit.Auditing",
            "FoundationKit.Security",
            "FoundationKit.Identity",
            "FoundationKit.Authorization",
            "FoundationKit.Workflow",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Blazor_does_not_depend_on_server_layers_or_server_capabilities()
    {
        AssertNoReferences(
            typeof(ApiResult).Assembly,
            "FoundationKit.Domain",
            "FoundationKit.Application",
            "FoundationKit.Infrastructure",
            "FoundationKit.WebApi",
            "FoundationKit.Auditing",
            "FoundationKit.Security",
            "FoundationKit.Identity",
            "FoundationKit.Authorization",
            "FoundationKit.Workflow",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Auditing_does_not_depend_on_infrastructure_web_or_upper_capabilities()
    {
        AssertNoReferences(
            typeof(AuditRecorder).Assembly,
            "FoundationKit.Infrastructure",
            "FoundationKit.WebApi",
            "FoundationKit.Blazor",
            "FoundationKit.Security",
            "FoundationKit.Identity",
            "FoundationKit.Authorization",
            "FoundationKit.Workflow",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Security_does_not_depend_on_identity_authorization_workflow_or_persistence_layers()
    {
        AssertNoReferences(
            typeof(TrustedProxySecurity).Assembly,
            "FoundationKit.Infrastructure",
            "FoundationKit.Blazor",
            "FoundationKit.Identity",
            "FoundationKit.Authorization",
            "FoundationKit.Workflow",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Identity_does_not_depend_on_authorization_workflow_or_persistence_layers()
    {
        AssertNoReferences(
            typeof(AccountSecurityOptions).Assembly,
            "FoundationKit.Infrastructure",
            "FoundationKit.Blazor",
            "FoundationKit.Authorization",
            "FoundationKit.Workflow",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Authorization_does_not_depend_on_workflow_persistence_or_product_layers()
    {
        AssertNoReferences(
            typeof(PermissionDefinition).Assembly,
            "FoundationKit.Infrastructure",
            "FoundationKit.Blazor",
            "FoundationKit.Workflow",
            "Microsoft.EntityFrameworkCore",
            "Athar.Domain",
            "Athar.Application",
            "Athar.Infrastructure",
            "Athar.Api");
    }

    [Fact]
    public void Workflow_depends_on_auditing_but_not_identity_authorization_or_product_layers()
    {
        AssertNoReferences(
            typeof(WorkflowDefinition).Assembly,
            "FoundationKit.Infrastructure",
            "FoundationKit.WebApi",
            "FoundationKit.Blazor",
            "FoundationKit.Security",
            "FoundationKit.Identity",
            "FoundationKit.Authorization",
            "Microsoft.EntityFrameworkCore",
            "Athar.Domain",
            "Athar.Application",
            "Athar.Infrastructure",
            "Athar.Api");
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
