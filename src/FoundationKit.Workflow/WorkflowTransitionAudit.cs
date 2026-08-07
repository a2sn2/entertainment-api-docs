using FoundationKit.Auditing;

namespace FoundationKit.Workflow;

public static class WorkflowTransitionAudit
{
    public const string TransitionedAction = "workflow.transitioned";

    public static AuditRequest CreateRequest(
        string subjectType,
        string? subjectId,
        WorkflowTransition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        return new AuditRequest(
            TransitionedAction,
            subjectType,
            subjectId,
            AuditOutcome.Succeeded,
            Attributes: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["workflow_id"] = transition.WorkflowId,
                ["transition_id"] = transition.TransitionId,
                ["from_state"] = transition.FromState,
                ["trigger"] = transition.Trigger,
                ["to_state"] = transition.ToState
            });
    }
}
