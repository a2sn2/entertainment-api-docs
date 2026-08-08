using FoundationKit.Authorization;
using Madar.Contracts.Security;

namespace Madar.Application.Security;

public static class MadarPermissions
{
    public const string ReadAllCases = "madar.cases.read-all";
    public const string AssignCases = "madar.cases.assign";
    public const string RouteCases = "madar.cases.route";
    public const string TransferCases = "madar.cases.transfer";
    public const string ReassignCases = "madar.cases.reassign";
    public const string ClaimCases = "madar.cases.claim";
    public const string ProgressAnyCase = "madar.cases.progress-any";
    public const string CloseCases = "madar.cases.close";
    public const string EvaluateSla = "madar.cases.sla.evaluate";
    public const string ApproveCases = "madar.cases.approve";
    public const string ManageDepartments = "madar.departments.manage";

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(ReadAllCases, "Read all cases"),
        new(AssignCases, "Assign cases"),
        new(RouteCases, "Route cases to departments"),
        new(TransferCases, "Transfer active cases between departments"),
        new(ReassignCases, "Reassign active cases between operators"),
        new(ClaimCases, "Claim cases from a department queue"),
        new(ProgressAnyCase, "Progress any assigned case"),
        new(CloseCases, "Close resolved cases"),
        new(EvaluateSla, "Evaluate case SLA breaches"),
        new(ApproveCases, "Approve sensitive case resolution"),
        new(ManageDepartments, "Manage departments and operator memberships")
    ];

    public static RolePermissionMap CreateRolePermissionMap() =>
        new(
        [
            new RolePermissionGrant(
                MadarRoles.Operator,
                [ClaimCases]),
            new RolePermissionGrant(
                MadarRoles.Supervisor,
                [ReadAllCases, AssignCases, RouteCases, TransferCases, ReassignCases, ProgressAnyCase, CloseCases, EvaluateSla, ApproveCases]),
            new RolePermissionGrant(
                MadarRoles.Administrator,
                All.Select(permission => permission.Id))
        ]);
}
