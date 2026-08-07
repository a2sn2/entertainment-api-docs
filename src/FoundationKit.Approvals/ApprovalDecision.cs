using FoundationKit.Workflow;

namespace FoundationKit.Approvals;

public enum ApprovalDecision
{
    Approve,
    Reject
}

public static class ApprovalDecisions
{
    public const string Approve = "approve";
    public const string Reject = "reject";

    public static bool TryParse(string? value, out ApprovalDecision decision)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case Approve:
                decision = ApprovalDecision.Approve;
                return true;
            case Reject:
                decision = ApprovalDecision.Reject;
                return true;
            default:
                decision = default;
                return false;
        }
    }

    public static string ToTrigger(ApprovalDecision decision) =>
        decision switch
        {
            ApprovalDecision.Approve => Approve,
            ApprovalDecision.Reject => Reject,
            _ => throw new ArgumentOutOfRangeException(nameof(decision))
        };

    public static bool TryResolve(
        WorkflowDefinition workflow,
        string currentState,
        string? decision,
        out ApprovalResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        if (!TryParse(decision, out var parsed))
        {
            resolution = null!;
            return false;
        }

        var trigger = ToTrigger(parsed);
        if (!workflow.TryResolve(currentState, trigger, out var transition))
        {
            resolution = null!;
            return false;
        }

        resolution = new ApprovalResolution(parsed, trigger, transition);
        return true;
    }
}

public sealed record ApprovalResolution(
    ApprovalDecision Decision,
    string DecisionToken,
    WorkflowTransition Transition);
