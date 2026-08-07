using FoundationKit.Application.Results;
using FoundationKit.Approvals;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using FoundationKit.Blazor.Api;
using FoundationKit.Domain.Primitives;
using FoundationKit.FeatureManagement;
using FoundationKit.Identity;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Localization;
using FoundationKit.Notifications;
using FoundationKit.Notifications.Smtp;
using FoundationKit.Security;
using FoundationKit.Settings;
using FoundationKit.WebApi.Results;
using FoundationKit.Workflow;
using Xunit;

namespace FoundationKit.Tests;

public sealed class LocalizationArchitectureTests
{
    [Fact]
    public void Existing_lower_and_peer_packages_do_not_depend_back_on_localization()
    {
        var assemblies = new[]
        {
            typeof(Entity<>).Assembly,
            typeof(Result).Assembly,
            typeof(EfRepository<,,>).Assembly,
            typeof(ResultHttpExtensions).Assembly,
            typeof(ApiResult).Assembly,
            typeof(AuditRecorder).Assembly,
            typeof(TrustedProxySecurity).Assembly,
            typeof(AccountSecurityOptions).Assembly,
            typeof(PermissionDefinition).Assembly,
            typeof(WorkflowDefinition).Assembly,
            typeof(ApprovalPolicy).Assembly,
            typeof(NotificationMessage).Assembly,
            typeof(SmtpNotificationSender).Assembly,
            typeof(SettingReader).Assembly,
            typeof(FeatureDefinition).Assembly
        };

        foreach (var assembly in assemblies)
        {
            AssertNoReferences(assembly, "FoundationKit.Localization");
        }
    }

    [Fact]
    public void Localization_is_provider_neutral_and_has_no_framework_or_product_dependency()
    {
        AssertNoReferences(
            typeof(SupportedCultureSet).Assembly,
            "FoundationKit.Domain",
            "FoundationKit.Application",
            "FoundationKit.Infrastructure",
            "FoundationKit.WebApi",
            "FoundationKit.Blazor",
            "FoundationKit.Auditing",
            "FoundationKit.Security",
            "FoundationKit.Identity",
            "FoundationKit.Authorization",
            "FoundationKit.Workflow",
            "FoundationKit.Approvals",
            "FoundationKit.Notifications",
            "FoundationKit.Notifications.Smtp",
            "FoundationKit.Settings",
            "FoundationKit.FeatureManagement",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
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
