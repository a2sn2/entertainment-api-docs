using FoundationKit.Authorization;

namespace FoundationKit.Approvals;

public enum ApprovalEligibility
{
    Allowed,
    PermissionDenied,
    MakerCheckerViolation
}

public static class ApprovalPolicy
{
    public static bool HasDecisionPermission(
        IAuthorizationEvaluator authorization,
        string permission)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return authorization.HasPermission(permission);
    }

    public static ApprovalEligibility Evaluate(
        IAuthorizationEvaluator authorization,
        string requiredPermission,
        string makerActorId,
        string checkerActorId)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredPermission);

        if (!authorization.HasPermission(requiredPermission))
        {
            return ApprovalEligibility.PermissionDenied;
        }

        return EvaluateMakerChecker(makerActorId, checkerActorId);
    }

    public static ApprovalEligibility EvaluateMakerChecker(
        string makerActorId,
        string checkerActorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(makerActorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkerActorId);

        return string.Equals(
            makerActorId.Trim(),
            checkerActorId.Trim(),
            StringComparison.OrdinalIgnoreCase)
            ? ApprovalEligibility.MakerCheckerViolation
            : ApprovalEligibility.Allowed;
    }
}
