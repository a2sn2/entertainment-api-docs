using FoundationKit.Authorization;
using Xunit;

namespace FoundationKit.Tests;

public sealed class AuthorizationCapabilityTests
{
    private const string ReadAllPermission = "sample.records.read-all";
    private const string ReviewPermission = "sample.records.review";

    [Fact]
    public void Permission_ids_are_normalized_and_invalid_ids_are_rejected()
    {
        var permission = new PermissionDefinition(
            " Sample.Records.Read-All ",
            "Read all records");

        Assert.Equal(ReadAllPermission, permission.Id);
        Assert.Throws<ArgumentException>(() =>
            new PermissionDefinition("sample records read", "Invalid"));
    }

    [Fact]
    public void Role_permission_map_deduplicates_permissions_and_roles()
    {
        var map = new RolePermissionMap(
        [
            new RolePermissionGrant("Administrator", [ReadAllPermission, ReadAllPermission]),
            new RolePermissionGrant("Auditor", [ReadAllPermission])
        ]);

        Assert.Equal(
            ["Administrator", "Auditor"],
            map.RolesFor(ReadAllPermission));
    }

    [Fact]
    public void Unauthenticated_subject_never_receives_role_permission()
    {
        var evaluator = CreateEvaluator(
            authenticated: false,
            userId: null,
            roles: ["Administrator"]);

        Assert.False(evaluator.HasPermission(ReadAllPermission));
    }

    [Fact]
    public void Authenticated_subject_receives_only_permissions_granted_to_its_roles()
    {
        var evaluator = CreateEvaluator(
            authenticated: true,
            userId: Guid.NewGuid(),
            roles: ["Administrator"]);

        Assert.True(evaluator.HasPermission(ReadAllPermission));
        Assert.False(evaluator.HasPermission(ReviewPermission));
    }

    [Fact]
    public void Owner_can_access_owned_resource_without_privileged_permission()
    {
        var ownerId = Guid.NewGuid();
        var evaluator = CreateEvaluator(
            authenticated: true,
            userId: ownerId,
            roles: []);

        Assert.True(evaluator.CanAccessOwnedResource(
            ownerId,
            ReadAllPermission));
    }

    [Fact]
    public void Privileged_subject_can_bypass_ownership_but_unprivileged_subject_cannot()
    {
        var ownerId = Guid.NewGuid();
        var administrator = CreateEvaluator(
            authenticated: true,
            userId: Guid.NewGuid(),
            roles: ["Administrator"]);
        var ordinaryUser = CreateEvaluator(
            authenticated: true,
            userId: Guid.NewGuid(),
            roles: []);

        Assert.True(administrator.CanAccessOwnedResource(
            ownerId,
            ReadAllPermission));
        Assert.False(ordinaryUser.CanAccessOwnedResource(
            ownerId,
            ReadAllPermission));
    }

    [Fact]
    public void Unknown_permission_fails_closed()
    {
        var evaluator = CreateEvaluator(
            authenticated: true,
            userId: Guid.NewGuid(),
            roles: ["Administrator"]);

        Assert.False(evaluator.HasPermission("sample.unknown"));
    }

    private static RolePermissionAuthorizationEvaluator CreateEvaluator(
        bool authenticated,
        Guid? userId,
        IReadOnlyCollection<string> roles)
    {
        var subject = new TestAuthorizationSubject(
            authenticated,
            userId,
            roles);
        var map = new RolePermissionMap(
        [
            new RolePermissionGrant(
                "Administrator",
                [ReadAllPermission])
        ]);

        return new RolePermissionAuthorizationEvaluator(subject, map);
    }

    private sealed class TestAuthorizationSubject(
        bool authenticated,
        Guid? userId,
        IReadOnlyCollection<string> roles) : IAuthorizationSubject
    {
        public bool IsAuthenticated => authenticated;

        public Guid? UserId => userId;

        public bool IsInRole(string role) =>
            roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
