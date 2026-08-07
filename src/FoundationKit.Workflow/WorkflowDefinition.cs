namespace FoundationKit.Workflow;

public sealed class WorkflowDefinition
{
    private readonly Dictionary<TransitionKey, WorkflowTransitionDefinition> _transitions;
    private readonly IReadOnlyList<WorkflowTransitionDefinition> _all;

    public WorkflowDefinition(
        string id,
        IEnumerable<WorkflowTransitionDefinition> transitions)
    {
        Id = WorkflowId.Normalize(id, nameof(id));
        ArgumentNullException.ThrowIfNull(transitions);

        var materialized = transitions.ToArray();
        if (materialized.Length == 0)
        {
            throw new ArgumentException(
                "A workflow definition must contain at least one transition.",
                nameof(transitions));
        }

        var transitionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _transitions = new Dictionary<TransitionKey, WorkflowTransitionDefinition>();

        foreach (var transition in materialized)
        {
            ArgumentNullException.ThrowIfNull(transition);

            if (!transitionIds.Add(transition.Id))
            {
                throw new ArgumentException(
                    $"Workflow '{Id}' contains duplicate transition ID '{transition.Id}'.",
                    nameof(transitions));
            }

            var key = new TransitionKey(transition.FromState, transition.Trigger);
            if (!_transitions.TryAdd(key, transition))
            {
                throw new ArgumentException(
                    $"Workflow '{Id}' contains an ambiguous transition from state '{transition.FromState}' for trigger '{transition.Trigger}'.",
                    nameof(transitions));
            }
        }

        _all = Array.AsReadOnly(materialized);
    }

    public string Id { get; }

    public IReadOnlyList<WorkflowTransitionDefinition> Transitions => _all;

    public bool CanTransition(string currentState, string trigger) =>
        TryResolve(currentState, trigger, out _);

    public bool TryResolve(
        string currentState,
        string trigger,
        out WorkflowTransition transition)
    {
        var normalizedState = WorkflowId.Normalize(currentState, nameof(currentState));
        var normalizedTrigger = WorkflowId.Normalize(trigger, nameof(trigger));

        if (_transitions.TryGetValue(
                new TransitionKey(normalizedState, normalizedTrigger),
                out var definition))
        {
            transition = WorkflowTransition.From(Id, definition);
            return true;
        }

        transition = null!;
        return false;
    }

    private readonly record struct TransitionKey(string State, string Trigger)
    {
        public bool Equals(TransitionKey other) =>
            string.Equals(State, other.State, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Trigger, other.Trigger, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(State),
                StringComparer.OrdinalIgnoreCase.GetHashCode(Trigger));
    }
}
