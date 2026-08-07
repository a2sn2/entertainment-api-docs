namespace FoundationKit.Workflow;

public sealed record WorkflowTransitionDefinition
{
    public WorkflowTransitionDefinition(
        string id,
        string fromState,
        string trigger,
        string toState)
    {
        Id = WorkflowId.Normalize(id, nameof(id));
        FromState = WorkflowId.Normalize(fromState, nameof(fromState));
        Trigger = WorkflowId.Normalize(trigger, nameof(trigger));
        ToState = WorkflowId.Normalize(toState, nameof(toState));
    }

    public string Id { get; }

    public string FromState { get; }

    public string Trigger { get; }

    public string ToState { get; }
}

public sealed record WorkflowTransition(
    string WorkflowId,
    string TransitionId,
    string FromState,
    string Trigger,
    string ToState)
{
    public static WorkflowTransition From(
        string workflowId,
        WorkflowTransitionDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return new WorkflowTransition(
            FoundationKit.Workflow.WorkflowId.Normalize(workflowId, nameof(workflowId)),
            definition.Id,
            definition.FromState,
            definition.Trigger,
            definition.ToState);
    }
}

public static class WorkflowId
{
    public static string Normalize(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 160)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Workflow identifiers cannot exceed 160 characters.");
        }

        if (!char.IsLetterOrDigit(normalized[0])
            || normalized.Any(character =>
                !(char.IsLetterOrDigit(character)
                  || character is '.' or ':' or '-' or '_')))
        {
            throw new ArgumentException(
                "Workflow identifiers must start with a letter or digit and contain only letters, digits, '.', ':', '-', or '_'.",
                parameterName);
        }

        return normalized;
    }
}
