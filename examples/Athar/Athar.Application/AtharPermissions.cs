using Athar.Contracts;
using FoundationKit.Authorization;

namespace Athar.Application;

public static class AtharPermissions
{
    public const string ReadAllInitiatives = "athar.initiatives.read-all";

    public const string ReviewInitiatives = "athar.initiatives.review";

    public const string ReadDashboard = "athar.dashboard.read";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(ReadAllInitiatives, "Read all initiatives"),
        new(ReviewInitiatives, "Review initiatives"),
        new(ReadDashboard, "Read administration dashboard")
    ];

    public static RolePermissionMap CreateRolePermissionMap() =>
        new(
        [
            new RolePermissionGrant(
                AtharRoles.Administrator,
                All.Select(permission => permission.Id))
        ]);
}
