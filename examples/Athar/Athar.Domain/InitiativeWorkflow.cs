using FoundationKit.Workflow;

namespace Athar.Domain;

public static class InitiativeWorkflow
{
    public const string WorkflowId = "athar.initiative-review";

    public static WorkflowDefinition Definition { get; } = new(
        WorkflowId,
        [
            new WorkflowTransitionDefinition(
                "approve-submitted-initiative",
                InitiativeStatuses.Submitted,
                InitiativeDecisions.Approve,
                InitiativeStatuses.Approved),
            new WorkflowTransitionDefinition(
                "reject-submitted-initiative",
                InitiativeStatuses.Submitted,
                InitiativeDecisions.Reject,
                InitiativeStatuses.Rejected)
        ]);
}
