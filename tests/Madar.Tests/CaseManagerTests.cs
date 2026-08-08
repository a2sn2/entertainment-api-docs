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

public sealed class CaseManagerTests
{
    [Fact]
    public async Task Create_AuthenticatedRequester_PersistsAndAuditsCase()
    {
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Requester);
        var fixture = CreateFixture(currentUser);

        var result = await fixture.Manager.CreateAsync(new CreateCaseRequest(
            "مشكلة تشغيلية جديدة",
            "وصف صالح ومفصل للحالة التشغيلية التي تحتاج إلى متابعة.",
            CaseTypes.OperationalIncident,
            CasePriorities.High));

        Assert.True(result.IsSuccess);
        Assert.Equal(CaseStatuses.New, result.Value.Status);
        Assert.Equal(currentUser.UserId, result.Value.CreatedByUserId);
        Assert.Single(fixture.Repository.Items);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        Assert.Contains(
            fixture.AuditSink.Events,
            auditEvent => auditEvent.Action == "madar.case.created");
    }

    [Fact]
    public async Task Assign_RequesterWithoutPermission_IsForbidden()
    {
        var requester = TestCurrentUser.Authenticated(MadarRoles.Requester);
        var assignee = Guid.NewGuid();
        var fixture = CreateFixture(requester, [assignee]);
        var item = CreateCase(requester.UserId!.Value, fixture.Clock.UtcNow);
        fixture.Repository.Seed(item);

        var result = await fixture.Manager.AssignAsync(
            item.Id,
            new AssignCaseRequest(assignee));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseApplicationErrors.AssignmentForbidden, result.Error);
        Assert.Equal(CaseStatuses.New, item.Status);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Assign_Supervisor_AssignsExistingUserAndAudits()
    {
        var supervisor = TestCurrentUser.Authenticated(MadarRoles.Supervisor);
        var creator = Guid.NewGuid();
        var assignee = Guid.NewGuid();
        var fixture = CreateFixture(supervisor, [assignee]);
        var item = CreateCase(creator, fixture.Clock.UtcNow);
        fixture.Repository.Seed(item);

        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddMinutes(1);
        var result = await fixture.Manager.AssignAsync(
            item.Id,
            new AssignCaseRequest(assignee));

        Assert.True(result.IsSuccess);
        Assert.Equal(CaseStatuses.Assigned, result.Value.Status);
        Assert.Equal(assignee, result.Value.AssignedToUserId);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        Assert.Contains(
            fixture.AuditSink.Events,
            auditEvent => auditEvent.Action == "madar.case.assigned");
    }

    [Fact]
    public async Task Transition_AssignedOperator_CanStartOwnCaseButAnotherOperatorCannot()
    {
        var operatorUserId = Guid.NewGuid();
        var creator = Guid.NewGuid();
        var item = CreateCase(creator, DateTimeOffset.Parse("2026-08-08T09:00:00Z"));
        Assert.True(item.Assign(
            operatorUserId,
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-08-08T09:01:00Z")).IsSuccess);

        var assignedOperator = TestCurrentUser.Authenticated(
            MadarRoles.Operator,
            operatorUserId);
        var assignedFixture = CreateFixture(assignedOperator);
        assignedFixture.Repository.Seed(item);
        assignedFixture.Clock.UtcNow = DateTimeOffset.Parse("2026-08-08T09:02:00Z");

        var started = await assignedFixture.Manager.TransitionAsync(
            item.Id,
            new TransitionCaseRequest(CaseTriggers.StartProgress));

        Assert.True(started.IsSuccess);
        Assert.Equal(CaseStatuses.InProgress, started.Value.Status);

        var otherItem = CreateCase(creator, DateTimeOffset.Parse("2026-08-08T10:00:00Z"));
        Assert.True(otherItem.Assign(
            operatorUserId,
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-08-08T10:01:00Z")).IsSuccess);

        var otherOperator = TestCurrentUser.Authenticated(MadarRoles.Operator);
        var otherFixture = CreateFixture(otherOperator);
        otherFixture.Repository.Seed(otherItem);

        var forbidden = await otherFixture.Manager.TransitionAsync(
            otherItem.Id,
            new TransitionCaseRequest(CaseTriggers.StartProgress));

        Assert.True(forbidden.IsFailure);
        Assert.Equal(CaseApplicationErrors.ProgressForbidden, forbidden.Error);
        Assert.Equal(CaseStatuses.Assigned, otherItem.Status);
    }

    [Fact]
    public async Task List_UsesUserScopeUnlessRoleHasReadAllPermission()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var operatorUser = TestCurrentUser.Authenticated(MadarRoles.Operator, userId);
        var fixture = CreateFixture(operatorUser);

        fixture.Repository.Seed(CreateCase(userId, fixture.Clock.UtcNow));
        fixture.Repository.Seed(CreateCase(otherUserId, fixture.Clock.UtcNow.AddMinutes(1)));
        var assigned = CreateCase(otherUserId, fixture.Clock.UtcNow.AddMinutes(2));
        Assert.True(assigned.Assign(
            userId,
            Guid.NewGuid(),
            fixture.Clock.UtcNow.AddMinutes(3)).IsSuccess);
        fixture.Repository.Seed(assigned);

        var scoped = await fixture.Manager.ListAsync();

        Assert.True(scoped.IsSuccess);
        Assert.Equal(2, scoped.Value.Count);

        var supervisor = TestCurrentUser.Authenticated(MadarRoles.Supervisor);
        var supervisorFixture = CreateFixture(
            supervisor,
            repository: fixture.Repository);

        var all = await supervisorFixture.Manager.ListAsync();

        Assert.True(all.IsSuccess);
        Assert.Equal(3, all.Value.Count);
    }

    private static Fixture CreateFixture(
        TestCurrentUser currentUser,
        IEnumerable<Guid>? knownUsers = null,
        FakeCaseRepository? repository = null)
    {
        repository ??= new FakeCaseRepository();
        var queryService = new FakeCaseQueryService(repository);
        var userDirectory = new FakeUserDirectory(knownUsers ?? []);
        var unitOfWork = new FakeUnitOfWork();
        var clock = new TestClock
        {
            UtcNow = DateTimeOffset.Parse("2026-08-08T09:00:00Z")
        };
        var auditSink = new CollectingAuditSink();
        var authorization = new RolePermissionAuthorizationEvaluator(
            currentUser,
            MadarPermissions.CreateRolePermissionMap());
        var auditRecorder = new AuditRecorder(
            auditSink,
            new TestAuditContextAccessor(currentUser),
            clock);

        return new Fixture(
            new CaseManager(
                currentUser,
                authorization,
                queryService,
                repository,
                userDirectory,
                unitOfWork,
                auditRecorder,
                clock),
            repository,
            unitOfWork,
            auditSink,
            clock);
    }

    private static Case CreateCase(Guid creator, DateTimeOffset createdUtc)
    {
        var result = Case.Create(
            creator,
            "طلب تشغيلي للاختبار",
            "هذا وصف صالح ومفصل لحالة تشغيلية مستخدمة في اختبارات مدير الحالات.",
            CaseTypes.InternalServiceRequest,
            CasePriorities.Medium,
            createdUtc);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private sealed record Fixture(
        CaseManager Manager,
        FakeCaseRepository Repository,
        FakeUnitOfWork UnitOfWork,
        CollectingAuditSink AuditSink,
        TestClock Clock);

    private sealed class TestCurrentUser : ICurrentUser, IAuthorizationSubject
    {
        private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);

        public bool IsAuthenticated { get; private init; }

        public Guid? UserId { get; private init; }

        public string? Email { get; private init; }

        public bool IsInRole(string role) => _roles.Contains(role);

        public static TestCurrentUser Authenticated(
            string role,
            Guid? userId = null)
        {
            var user = new TestCurrentUser
            {
                IsAuthenticated = true,
                UserId = userId ?? Guid.NewGuid(),
                Email = "madar-test@example.test"
            };
            user._roles.Add(role);
            return user;
        }
    }

    private sealed class FakeCaseRepository : IRepository<Case, Guid>
    {
        public Dictionary<Guid, Case> Items { get; } = [];

        public void Seed(Case item) => Items[item.Id] = item;

        public Task<Case?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
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

        public Task AddAsync(
            Case entity,
            CancellationToken cancellationToken = default)
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

    private sealed class FakeCaseQueryService(FakeCaseRepository repository)
        : ICaseQueryService
    {
        public Task<CaseDto?> GetByIdAsync(
            Guid caseId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                repository.Items.TryGetValue(caseId, out var item)
                    ? ToDto(item)
                    : null);

        public Task<IReadOnlyList<CaseDto>> ListForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CaseDto>>(
                repository.Items.Values
                    .Where(item =>
                        item.CreatedByUserId == userId
                        || item.AssignedToUserId == userId)
                    .Select(ToDto)
                    .ToArray());

        public Task<IReadOnlyList<CaseDto>> ListAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CaseDto>>(
                repository.Items.Values.Select(ToDto).ToArray());

        private static CaseDto ToDto(Case item) =>
            new(
                item.Id,
                item.CreatedByUserId,
                item.Title,
                item.Description,
                item.CaseType,
                item.Priority,
                item.Status,
                item.AssignedToUserId,
                item.CreatedUtc,
                item.UpdatedUtc,
                item.ResolvedUtc,
                item.ClosedUtc);
    }

    private sealed class FakeUserDirectory(IEnumerable<Guid> users) : IUserDirectory
    {
        private readonly HashSet<Guid> _users = new(users);

        public Task<bool> ExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Contains(userId));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
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
            "madar-test-correlation",
            null,
            "madar-tests");
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
