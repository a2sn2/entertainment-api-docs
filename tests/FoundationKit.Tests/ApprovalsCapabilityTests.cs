using FoundationKit.Approvals;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using FoundationKit.Workflow;
using Xunit;

namespace FoundationKit.Tests;

public sealed class ApprovalsCapabilityTests
{
    private const string ReviewPermission = "sample.records.review";

    [Theory]
    [InlineData("approve", ApprovalDecision.Approve, ApprovalDecisions.Approve)]
    [InlineData(" APPROVE ", ApprovalDecision.Approve, ApprovalDecisions.Approve)]
    [InlineData("reject", ApprovalDecision.Reject, ApprovalDecisions.Reject)]
    [InlineData(" REJECT ", ApprovalDecision.Reject, ApprovalDecisions.Reject)]
    public void Decisions_are_normalized(
        string input,
        ApprovalDecision expectedDecision,
        string expectedToken)
    {
        Assert.True(ApprovalDecisions.TryParse(input, out var decision));
        Assert.Equal(expectedDecision, decision);
        Assert.Equal(expectedToken, ApprovalDecisions.ToTrigger(decision));
    }

    [Fact]
    public void Unknown_decision_fails_closed()
    {
        Assert.False(ApprovalDecisions.TryParse("escalate", out _));
    }

    [Fact]
    public void Approval_resolution_composes_a_workflow_transition()
    {
        var workflow = CreateWorkflow();

        Assert.True(ApprovalDecisions.TryResolve(
            workflow,
            "submitted",
            " APPROVE ",
            out var resolution));

        Assert.Equal(ApprovalDecision.Approve, resolution.Decision);
        Assert.Equal(ApprovalDecisions.Approve, resolution.DecisionToken);
        Assert.Equal("approved", resolution.Transition.ToState);
    }

    [Fact]
    public void Missing_permission_is_denied_before_maker_checker()
    {
        var evaluator = CreateEvaluator(hasReviewPermission: false);

        var eligibility = ApprovalPolicy.Evaluate(
            evaluator,
            ReviewPermission,
            "actor-1",
            "actor-2");

        Assert.Equal(ApprovalEligibility.PermissionDenied, eligibility);
    }

    [Fact]
    public void Same_maker_and_checker_is_rejected()
    {
        var evaluator = CreateEvaluator(hasReviewPermission: true);

        var eligibility = ApprovalPolicy.Evaluate(
            evaluator,
            ReviewPermission,
            " Actor-1 ",
            "actor-1");

        Assert.Equal(ApprovalEligibility.MakerCheckerViolation, eligibility);
    }

    [Fact]
    public void Authorized_distinct_checker_is_allowed()
    {
        var evaluator = CreateEvaluator(hasReviewPermission: true);

        var eligibility = ApprovalPolicy.Evaluate(
            evaluator,
            ReviewPermission,
            "actor-1",
            "actor-2");

        Assert.Equal(ApprovalEligibility.Allowed, eligibility);
    }

    [Fact]
    public void Approval_resolution_creates_bounded_audit_intent()
    {
        var workflow = CreateWorkflow();
        Assert.True(ApprovalDecisions.TryResolve(
            workflow,
            "submitted",
            "approve",
            out var resolution));

        var request = ApprovalDecisionAudit.CreateRequest(
            "initiative",
            "initiative-1",
            resolution);
        var auditEvent = AuditEvent.Create(
            request,
            new AuditContext("checker-1", "corr-1", null, "tests"),
            new DateTimeOffset(2026, 8, 7, 19, 0, 0, TimeSpan.Zero));

        Assert.Equal(ApprovalDecisionAudit.DecidedAction, auditEvent.Action);
        Assert.Equal("approve", auditEvent.Attributes["decision"]);
        Assert.Equal("sample.review", auditEvent.Attributes["workflow_id"]);
        Assert.Equal("approve-submitted", auditEvent.Attributes["transition_id"]);
        Assert.Equal("submitted", auditEvent.Attributes["from_state"]);
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

    private static RolePermissionAuthorizationEvaluator CreateEvaluator(
        bool hasReviewPermission)
    {
        var roles = hasReviewPermission
            ? new[] { "Reviewer" }
            : Array.Empty<string>();
        var subject = new TestAuthorizationSubject(
            authenticated: true,
            Guid.NewGuid(),
            roles);
        var map = new RolePermissionMap(
        [
            new RolePermissionGrant("Reviewer", [ReviewPermission])
        ]);

        return new RolePermissionAuthorizationEvaluator(subject, map);
    }

    private sealed class TestAuthorizationSubject(
        bool authenticated,
        Guid? userId,
        IReadOnlyCollection<string> roles) : IAuthorizationSubject
    {
        public bool IsAuthenticated => authenticated;

        public Guid? UserId => userId;

        public bool IsInRole(string role) =>
            roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
