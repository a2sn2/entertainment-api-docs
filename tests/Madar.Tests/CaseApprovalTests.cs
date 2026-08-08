using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Application.Cases;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Contracts.Security;
using Madar.Domain.Cases;
using Xunit;

namespace Madar.Tests;

public sealed class CaseApprovalTests
{
    [Fact]
    public void Decide_TrimsBoundedNotes_AndPreventsSelfReview()
    {
        var requester = Guid.NewGuid();
        var creation = CaseApproval.Create(Guid.NewGuid(), requester, Utc(9));
        Assert.True(creation.IsSuccess);

        var selfReview = creation.Value.Decide(
            requester,
            "approve",
            "لا يجب اعتماد الطلب ذاتيًا",
            Utc(9, 1));

        Assert.True(selfReview.IsFailure);
        Assert.Equal(CaseApprovalErrors.SelfReviewNotAllowed, selfReview.Error);
        Assert.Equal(CaseApprovalStatuses.Pending, creation.Value.Status);

        var approved = creation.Value.Decide(
            Guid.NewGuid(),
            "approve",
            "  ملاحظة مراجعة محدودة  ",
            Utc(9, 2));

        Assert.True(approved.IsSuccess);
        Assert.Equal(CaseApprovalStatuses.Approved, creation.Value.Status);
        Assert.Equal("ملاحظة مراجعة محدودة", creation.Value.DecisionNotes);
    }

    [Fact]
    public async Task Request_AssignedOperator_PersistsAndAudits()
    {
        var operatorId = Guid.NewGuid();
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Operator, operatorId);
        var cases = new FakeCaseRepository();
        var approvals = new FakeApprovalStore();
        var item = CreateSensitiveInProgressCase(operatorId);
        cases.Seed(item);
        var fixture = CreateFixture(currentUser, cases, approvals);

        var result = await fixture.Manager.RequestAsync(item.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(CaseApprovalStatuses.Pending, result.Value.Status);
        Assert.Equal(operatorId, result.Value.RequestedByUserId);
        Assert.Single(approvals.Items);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        Assert.Contains(
            fixture.AuditSink.Events,
            entry => entry.Action == "madar.case.approval-requested");
    }

    [Fact]
    public async Task Decide_RequesterWithoutPermission_ReturnsPermissionBeforeMakerChecker()
    {
        var operatorId = Guid.NewGuid();
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Operator, operatorId);
        var cases = new FakeCaseRepository();
        var approvals = new FakeApprovalStore();
        var item = CreateSensitiveInProgressCase(operatorId);
        cases.Seed(item);
        var approval = CreateApproval(item.Id, operatorId);
        approvals.Seed(approval);
        var fixture = CreateFixture(currentUser, cases, approvals);

        var result = await fixture.Manager.DecideAsync(
            item.Id,
            approval.Id,
            new DecideCaseApprovalRequest("approve", null));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseApprovalApplicationErrors.DecisionForbidden, result.Error);
        Assert.Equal(CaseApprovalStatuses.Pending, approval.Status);
    }

    [Fact]
    public async Task Decide_DifferentSupervisor_ApprovesWithoutAuditNoteLeakage()
    {
        var requester = Guid.NewGuid();
        var supervisor = TestCurrentUser.Authenticated(MadarRoles.Supervisor);
        var cases = new FakeCaseRepository();
        var approvals = new FakeApprovalStore();
        var item = CreateSensitiveInProgressCase(requester);
        cases.Seed(item);
        var approval = CreateApproval(item.Id, requester);
        approvals.Seed(approval);
        var fixture = CreateFixture(supervisor, cases, approvals);
        const string notes = "سبب اعتماد داخلي لا يجب نسخه إلى سجل التدقيق";

        var result = await fixture.Manager.DecideAsync(
            item.Id,
            approval.Id,
            new DecideCaseApprovalRequest("approve", notes));

        Assert.True(result.IsSuccess);
        Assert.Equal(CaseApprovalStatuses.Approved, result.Value.Status);
        Assert.Equal(notes, result.Value.DecisionNotes);
        var audit = Assert.Single(
            fixture.AuditSink.Events,
            entry => entry.Action == "madar.case.approval-decided");
        Assert.Equal("approve", audit.Attributes["decision"]);
        Assert.False(audit.Attributes.Values.Any(
            value => value.Contains("سبب اعتماد داخلي", StringComparison.Ordinal)));
        Assert.False(audit.Attributes.Keys.Any(
            key => string.Equals(key, "notes", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Request_AfterRejectedApproval_CreatesNewPendingApproval()
    {
        var operatorId = Guid.NewGuid();
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Operator, operatorId);
        var cases = new FakeCaseRepository();
        var approvals = new FakeApprovalStore();
        var item = CreateSensitiveInProgressCase(operatorId);
        cases.Seed(item);
        var rejected = CreateApproval(item.Id, operatorId);
        Assert.True(rejected.Decide(
            Guid.NewGuid(),
            "reject",
            null,
            Utc(9, 3)).IsSuccess);
        approvals.Seed(rejected);
        var fixture = CreateFixture(currentUser, cases, approvals);

        var result = await fixture.Manager.RequestAsync(item.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(CaseApprovalStatuses.Pending, result.Value.Status);
        Assert.Equal(2, approvals.Items.Count);
    }

    [Fact]
    public void Requirement_IsLimitedToSensitiveCaseTypes()
    {
        Assert.True(CaseApprovalRequirement.IsRequired(CaseTypes.AccessRequest));
        Assert.True(CaseApprovalRequirement.IsRequired(CaseTypes.ComplianceCase));
        Assert.False(CaseApprovalRequirement.IsRequired(CaseTypes.OperationalIncident));
        Assert.False(CaseApprovalRequirement.IsRequired(CaseTypes.InternalServiceRequest));
    }

    private static Fixture CreateFixture(
        TestCurrentUser currentUser,
        FakeCaseRepository cases,
        FakeApprovalStore approvals)
    {
        var unitOfWork = new FakeUnitOfWork();
        var auditSink = new CollectingAuditSink();
        var clock = new TestClock { UtcNow = Utc(10) };
        var authorization = new RolePermissionAuthorizationEvaluator(
            currentUser,
            MadarPermissions.CreateRolePermissionMap());
        var auditRecorder = new AuditRecorder(
            auditSink,
            new TestAuditContextAccessor(currentUser),
            clock);

        return new Fixture(
            new CaseApprovalManager(
                currentUser,
                authorization,
                cases,
                approvals,
                approvals,
                unitOfWork,
                auditRecorder,
                clock),
            unitOfWork,
            auditSink);
    }

    private static Case CreateSensitiveInProgressCase(Guid assignee)
    {
        var creation = Case.Create(
            Guid.NewGuid(),
            "طلب صلاحية حساس",
            "وصف صالح لطلب صلاحية حساس يحتاج إلى مراجعة مستقلة قبل تسجيل الحل.",
            CaseTypes.AccessRequest,
            CasePriorities.High,
            Utc(9));
        Assert.True(creation.IsSuccess);
        Assert.True(creation.Value.Assign(assignee, Guid.NewGuid(), Utc(9, 1)).IsSuccess);
        Assert.True(creation.Value.StartProgress(assignee, Utc(9, 2)).IsSuccess);
        return creation.Value;
    }

    private static CaseApproval CreateApproval(Guid caseId, Guid requester)
    {
        var result = CaseApproval.Create(caseId, requester, Utc(9, 2));
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static DateTimeOffset Utc(int hour, int minute = 0) =>
        new(2026, 8, 8, hour, minute, 0, TimeSpan.Zero);

    private sealed record Fixture(
        CaseApprovalManager Manager,
        FakeUnitOfWork UnitOfWork,
        CollectingAuditSink AuditSink);

    private sealed class TestCurrentUser : ICurrentUser, IAuthorizationSubject
    {
        private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);

        public bool IsAuthenticated { get; private init; }
        public Guid? UserId { get; private init; }
        public string? Email { get; private init; }
        public bool IsInRole(string role) => _roles.Contains(role);

        public static TestCurrentUser Authenticated(string role, Guid? userId = null)
        {
            var user = new TestCurrentUser
            {
                IsAuthenticated = true,
                UserId = userId ?? Guid.NewGuid(),
                Email = "approval-test@example.test"
            };
            user._roles.Add(role);
            return user;
        }
    }

    private sealed class FakeCaseRepository : IRepository<Case, Guid>
    {
        public Dictionary<Guid, Case> Items { get; } = [];
        public void Seed(Case item) => Items[item.Id] = item;

        public Task<Case?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(id));

        public Task<Case?> FirstOrDefaultAsync(
            ISpecification<Case> specification,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Case>> ListAsync(
            ISpecification<Case>? specification = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Case>>(Items.Values.ToArray());

        public Task<int> CountAsync(
            ISpecification<Case>? specification = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count);

        public Task AddAsync(Case entity, CancellationToken cancellationToken = default)
        {
            Items[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(
            IEnumerable<Case> entities,
            CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
                Items[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public void Remove(Case entity) => Items.Remove(entity.Id);
        public void RemoveRange(IEnumerable<Case> entities)
        {
            foreach (var entity in entities)
                Items.Remove(entity.Id);
        }
    }

    private sealed class FakeApprovalStore : ICaseApprovalRepository, ICaseApprovalQueryService
    {
        public Dictionary<Guid, CaseApproval> Items { get; } = [];
        public void Seed(CaseApproval approval) => Items[approval.Id] = approval;

        public Task<CaseApproval?> GetByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.GetValueOrDefault(approvalId));

        public Task AddAsync(
            CaseApproval approval,
            CancellationToken cancellationToken = default)
        {
            Items[approval.Id] = approval;
            return Task.CompletedTask;
        }

        Task<CaseApprovalDto?> ICaseApprovalQueryService.GetByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                Items.TryGetValue(approvalId, out var approval)
                    ? ToDto(approval)
                    : null);

        public Task<CaseApprovalDto?> GetLatestForCaseAsync(
            Guid caseId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Items.Values
                    .Where(item => item.CaseId == caseId)
                    .OrderByDescending(item => item.RequestedUtc)
                    .ThenByDescending(item => item.Id)
                    .Select(ToDto)
                    .FirstOrDefault());

        public Task<IReadOnlyList<CaseApprovalDto>> ListForCaseAsync(
            Guid caseId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CaseApprovalDto>>(
                Items.Values
                    .Where(item => item.CaseId == caseId)
                    .OrderBy(item => item.RequestedUtc)
                    .ThenBy(item => item.Id)
                    .Select(ToDto)
                    .ToArray());

        private static CaseApprovalDto ToDto(CaseApproval approval) =>
            new(
                approval.Id,
                approval.CaseId,
                approval.RequestedByUserId,
                "طالب الاعتماد",
                approval.RequestedUtc,
                approval.Status,
                approval.ReviewedByUserId,
                approval.ReviewedByUserId.HasValue ? "المراجع" : null,
                approval.DecidedUtc,
                approval.DecisionNotes);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class CollectingAuditSink : IAuditSink
    {
        public List<AuditEvent> Events { get; } = [];
        public ValueTask WriteAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestAuditContextAccessor(TestCurrentUser currentUser)
        : IAuditContextAccessor
    {
        public AuditContext Current => new(
            currentUser.UserId?.ToString("D"),
            "madar-approval-test-correlation",
            null,
            "madar-approval-tests");
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
