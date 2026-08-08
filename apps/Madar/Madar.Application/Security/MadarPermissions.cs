using FoundationKit.Authorization;
using Madar.Contracts.Security;

namespace Madar.Application.Security;

public static class MadarPermissions
{
    public const string ReadAllCases = "madar.cases.read-all";
    public const string AssignCases = "madar.cases.assign";
    public const string ProgressAnyCase = "madar.cases.progress-any";
    public const string CloseCases = "madar.cases.close";
    public const string EvaluateSla = "madar.cases.sla.evaluate";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(ReadAllCases, "Read all cases"),
        new(AssignCases, "Assign cases"),
        new(ProgressAnyCase, "Progress any assigned case"),
        new(CloseCases, "Close resolved cases"),
        new(EvaluateSla, "Evaluate case SLA breaches")
    ];

    public static RolePermissionMap CreateRolePermissionMap() =>
        new(
        [
            new RolePermissionGrant(
                MadarRoles.Supervisor,
                [ReadAllCases, AssignCases, ProgressAnyCase, CloseCases, EvaluateSla]),
            new RolePermissionGrant(
                MadarRoles.Administrator,
                All.Select(permission => permission.Id))
        ]);
}
