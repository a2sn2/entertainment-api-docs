using FoundationKit.Application.Results;
using FoundationKit.Approvals;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using FoundationKit.Blazor.Api;
using FoundationKit.Domain.Primitives;
using FoundationKit.FeatureManagement;
using FoundationKit.Identity;
using FoundationKit.Infrastructure.Persistence;
using FoundationKit.Notifications;
using FoundationKit.Notifications.Smtp;
using FoundationKit.Security;
using FoundationKit.Settings;
using FoundationKit.WebApi.Results;
using FoundationKit.Workflow;
using Xunit;

namespace FoundationKit.Tests;

public sealed class SettingsFeatureArchitectureTests
{
    [Fact]
    public void Existing_lower_and_peer_packages_do_not_depend_back_on_settings_or_feature_management()
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
            typeof(SmtpNotificationSender).Assembly
        };

        foreach (var assembly in assemblies)
        {
            AssertNoReferences(
                assembly,
                "FoundationKit.Settings",
                "FoundationKit.FeatureManagement");
        }
    }

    [Fact]
    public void Settings_is_provider_neutral_and_has_no_framework_or_product_dependency()
    {
        AssertNoReferences(
            typeof(SettingReader).Assembly,
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
            "FoundationKit.FeatureManagement",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Athar.Domain",
            "Athar.Application",
            "Athar.Infrastructure",
            "Athar.Api");
    }

    [Fact]
    public void Feature_management_depends_on_settings_only_within_foundationkit()
    {
        var references = typeof(FeatureDefinition).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.Contains(
            references,
            reference => reference.Equals(
                "FoundationKit.Settings",
                StringComparison.OrdinalIgnoreCase));

        AssertNoReferences(
            typeof(FeatureDefinition).Assembly,
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
