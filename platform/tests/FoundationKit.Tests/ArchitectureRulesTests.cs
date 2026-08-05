using System.Reflection;
using EntertainmentDocs.Application;
using EntertainmentDocs.Domain.Documents;
using EntertainmentDocs.Infrastructure;
using FoundationKit.Application.Results;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Tests;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void Foundation_domain_has_no_framework_or_outer_layer_dependencies()
    {
        AssertDoesNotReference(
            typeof(Entity<>).Assembly,
            "FoundationKit.Application",
            "FoundationKit.Infrastructure",
            "FoundationKit.WebApi",
            "FoundationKit.Blazor",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Product_domain_has_no_application_infrastructure_or_api_dependencies()
    {
        AssertDoesNotReference(
            typeof(DocumentationDocument).Assembly,
            "EntertainmentDocs.Application",
            "EntertainmentDocs.Infrastructure",
            "EntertainmentDocs.Api",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Product_application_has_no_infrastructure_or_api_dependencies()
    {
        AssertDoesNotReference(
            typeof(DependencyInjection).Assembly,
            "EntertainmentDocs.Infrastructure",
            "EntertainmentDocs.Api",
            "Microsoft.EntityFrameworkCore.SqlServer");
    }

    [Fact]
    public void Contracts_are_transport_only()
    {
        AssertDoesNotReference(
            typeof(EntertainmentDocs.Contracts.Documents.CreateDocumentRequest).Assembly,
            "EntertainmentDocs.Domain",
            "EntertainmentDocs.Application",
            "EntertainmentDocs.Infrastructure",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Infrastructure_may_reference_inward_but_not_api_or_frontend()
    {
        AssertDoesNotReference(
            typeof(EntertainmentDocs.Infrastructure.DependencyInjection).Assembly,
            "EntertainmentDocs.Api",
            "EntertainmentDocs.Admin",
            "EntertainmentDocs.Client");
    }

    [Fact]
    public void Foundation_application_does_not_reference_outer_adapters()
    {
        AssertDoesNotReference(
            typeof(Result).Assembly,
            "FoundationKit.Infrastructure",
            "FoundationKit.WebApi",
            "FoundationKit.Blazor",
            "Microsoft.EntityFrameworkCore");
    }

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbiddenAssemblies)
    {
        var references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var forbidden in forbiddenAssemblies)
            Assert.DoesNotContain(forbidden, references);
    }
}
