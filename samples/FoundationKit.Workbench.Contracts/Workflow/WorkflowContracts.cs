namespace FoundationKit.Workbench.Contracts.Workflow;

public static class WorkflowStatuses
{
    public const string Submitted = "submitted";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static bool IsFinal(string? status) =>
        string.Equals(status, Approved, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, Rejected, StringComparison.OrdinalIgnoreCase);
}

public static class ReviewDecisions
{
    public const string Approve = "approve";
    public const string Reject = "reject";

    public static bool IsSupported(string? decision) =>
        string.Equals(decision, Approve, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(decision, Reject, StringComparison.OrdinalIgnoreCase);
}
