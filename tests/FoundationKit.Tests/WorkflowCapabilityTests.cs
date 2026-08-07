using System.Globalization;
using FoundationKit.Auditing;
using FoundationKit.Workflow;
using Xunit;

namespace FoundationKit.Tests;

public sealed class WorkflowCapabilityTests
{
    [Fact]
    public void Definition_resolves_a_deterministic_transition()
    {
        var workflow = CreateWorkflow();

        Assert.True(workflow.TryResolve(" submitted ", " APPROVE ", out var transition));
        Assert.Equal("sample.review", transition.WorkflowId);
        Assert.Equal("approve-submitted", transition.TransitionId);
        Assert.Equal("submitted", transition.FromState);
        Assert.Equal("approve", transition.Trigger);
        Assert.Equal("approved", transition.ToState);
    }

    [Fact]
    public void Unknown_state_or_trigger_fails_closed()
    {
        var workflow = CreateWorkflow();

        Assert.False(workflow.CanTransition("approved", "approve"));
        Assert.False(workflow.CanTransition("submitted", "unknown"));
    }

    [Fact]
    public void Ambiguous_transition_key_is_rejected()
    {
        var transitions = new[]
        {
            new WorkflowTransitionDefinition(
                "first",
                "submitted",
                "approve",
                "approved"),
            new WorkflowTransitionDefinition(
                "second",
                "submitted",
                "approve",
                "other")
        };

        var exception = Assert.Throws<ArgumentException>(() =>
            new WorkflowDefinition("sample.review", transitions));

        Assert.Contains("ambiguous", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Duplicate_transition_ids_are_rejected()
    {
        var transitions = new[]
        {
            new WorkflowTransitionDefinition(
                "review",
                "submitted",
                "approve",
                "approved"),
            new WorkflowTransitionDefinition(
                "review",
                "submitted",
                "reject",
                "rejected")
        };

        var exception = Assert.Throws<ArgumentException>(() =>
            new WorkflowDefinition("sample.review", transitions));

        Assert.Contains("duplicate transition ID", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Transition_collection_is_read_only_to_consumers()
    {
        var workflow = CreateWorkflow();

        Assert.IsAssignableFrom<IReadOnlyList<WorkflowTransitionDefinition>>(workflow.Transitions);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<WorkflowTransitionDefinition>)workflow.Transitions).Add(
                new WorkflowTransitionDefinition("x", "a", "b", "c")));
    }

    [Fact]
    public void Workflow_transition_creates_a_bounded_audit_request()
    {
        var workflow = CreateWorkflow();
        Assert.True(workflow.TryResolve("submitted", "approve", out var transition));

        var request = WorkflowTransitionAudit.CreateRequest(
            "initiative",
            Guid.NewGuid().ToString(),
            transition);
        var auditEvent = AuditEvent.Create(
            request,
            new AuditContext("reviewer-1", "corr-1", null, "tests"),
            DateTimeOffset.Parse(
                "2026-08-07T18:00:00Z",
                CultureInfo.InvariantCulture));

        Assert.Equal(WorkflowTransitionAudit.TransitionedAction, auditEvent.Action);
        Assert.Equal("sample.review", auditEvent.Attributes["workflow_id"]);
        Assert.Equal("approve-submitted", auditEvent.Attributes["transition_id"]);
        Assert.Equal("submitted", auditEvent.Attributes["from_state"]);
        Assert.Equal("approve", auditEvent.Attributes["trigger"]);
        Assert.Equal("approved", auditEvent.Attributes["to_state"]);
    }

    private static WorkflowDefinition CreateWorkflow() => new(
        "sample.review",
        [
            new WorkflowTransitionDefinition(
                "approve-submitted",
                "submitted",
                "approve",
                "approved"),
            new WorkflowTransitionDefinition(
                "reject-submitted",
                "submitted",
                "reject",
                "rejected")
        ]);
}
