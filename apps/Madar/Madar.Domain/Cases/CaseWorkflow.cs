using FoundationKit.Workflow;

namespace Madar.Domain.Cases;

public static class CaseWorkflow
{
    public const string WorkflowId = "madar.case-lifecycle";

    public static WorkflowDefinition Definition { get; } = new(
        WorkflowId,
        [
            new WorkflowTransitionDefinition(
                "assign-new-case",
                CaseStatuses.New,
                CaseTriggers.Assign,
                CaseStatuses.Assigned),
            new WorkflowTransitionDefinition(
                "start-assigned-case",
                CaseStatuses.Assigned,
                CaseTriggers.StartProgress,
                CaseStatuses.InProgress),
            new WorkflowTransitionDefinition(
                "resolve-in-progress-case",
                CaseStatuses.InProgress,
                CaseTriggers.Resolve,
                CaseStatuses.Resolved),
            new WorkflowTransitionDefinition(
                "close-resolved-case",
                CaseStatuses.Resolved,
                CaseTriggers.Close,
                CaseStatuses.Closed)
        ]);
}
