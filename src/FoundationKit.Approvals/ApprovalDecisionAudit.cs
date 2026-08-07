using FoundationKit.Auditing;

namespace FoundationKit.Approvals;

public static class ApprovalDecisionAudit
{
    public const string DecidedAction = "approval.decided";

    public static AuditRequest CreateRequest(
        string subjectType,
        string? subjectId,
        ApprovalResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        return new AuditRequest(
            DecidedAction,
            subjectType,
            subjectId,
            AuditOutcome.Succeeded,
            Attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["decision"] = resolution.DecisionToken,
                ["workflow_id"] = resolution.Transition.WorkflowId,
                ["transition_id"] = resolution.Transition.TransitionId,
                ["from_state"] = resolution.Transition.FromState,
                ["to_state"] = resolution.Transition.ToState
            });
    }
}
