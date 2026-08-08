using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Application.Cases;
using Madar.Application.Organization;
using Madar.Application.Security;
using Madar.Contracts.Cases;
using Madar.Contracts.Organization;
using Madar.Contracts.Security;
using Madar.Domain.Cases;
using Xunit;

namespace Madar.Tests;

public sealed class CaseRoutingTests
{
    [Fact]
    public async Task Route_Supervisor_RoutesNewCaseWithoutChangingLifecycleState()
    {
        var supervisor = TestUser.Authenticated(MadarRoles.Supervisor);
        var department = ActiveDepartment();
        var shared = SharedFixture.Create([department]);
        var item = CreateCase(Guid.NewGuid(), shared.Clock.UtcNow);
        shared.Repository.Seed(item);
        var manager = shared.CreateRoutingManager(supervisor);

        var result = await manager.RouteAsync(
            item.Id,
            new RouteCaseRequest(department.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(CaseStatuses.New, result.Value.Status);
        Assert.Equal(department.Id, result.Value.DepartmentId);
        Assert.Equal(shared.Clock.UtcNow, result.Value.RoutedUtc);
        Assert.Null(result.Value.AssignedToUserId);
        var audit = Assert.Single(
            shared.AuditSink.Events,
            entry => entry.Action == "madar.case.routed");
        Assert.Equal(department.Id.ToString("D"), audit.Attributes["departmentId"]);
        Assert.Single(audit.Attributes);
    }

    [Fact]
    public async Task Route_AssignedCase_IsRejectedByDomainInvariant()
    {
        var supervisor = TestUser.Authenticated(MadarRoles.Supervisor);
        var operatorId = Guid.NewGuid();
        var department = ActiveDepartment();
        var shared = SharedFixture.Create([department], [operatorId]);
        var item = CreateCase(Guid.NewGuid(), shared.Clock.UtcNow);
        Assert.True(item.Assign(operatorId, supervisor.UserId!.Value, shared.Clock.UtcNow).IsSuccess);
        shared.Repository.Seed(item);

        var result = await shared.CreateRoutingManager(supervisor).RouteAsync(
            item.Id,
            new RouteCaseRequest(department.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseErrors.InvalidRoutingState, result.Error);
        Assert.Null(item.DepartmentId);
    }

    [Fact]
    public async Task Queue_OperatorMember_SeesOnlyRoutedUnassignedNewCases()
    {
        var operatorId = Guid.NewGuid();
        var operatorUser = TestUser.Authenticated(MadarRoles.Operator, operatorId);
        var department = ActiveDepartment();
        var shared = SharedFixture.Create([department], [operatorId]);
        shared.Departments.AddMember(department.Id, operatorId);

        var queued = CreateCase(Guid.NewGuid(), shared.Clock.UtcNow);
        Assert.True(queued.RouteToDepartment(
            department.Id,
            Guid.NewGuid(),
            shared.Clock.UtcNow).IsSuccess);
        shared.Repository.Seed(queued);

        var unrelated = CreateCase(Guid.NewGuid(), shared.Clock.UtcNow.AddMinutes(1));
        shared.Repository.Seed(unrelated);

        var result = await shared.CreateRoutingManager(operatorUser)
            .GetQueueAsync(department.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Cases);
        Assert.Equal(queued.Id, result.Value.Cases[0].Id);
    }

    [Fact]
    public async Task Queue_NonMember_IsForbiddenWithoutLeakingCases()
    {
        var operatorUser = TestUser.Authenticated(MadarRoles.Operator);
        var department = ActiveDepartment();
        var shared = SharedFixture.Create([department], [operatorUser.UserId!.Value]);
        var queued = CreateCase(Guid.NewGuid(), shared.Clock.UtcNow);
        Assert.True(queued.RouteToDepartment(
            department.Id,
            Guid.NewGuid(),
            shared.Clock.UtcNow).IsSuccess);
        shared.Repository.Seed(queued);

        var result = await shared.CreateRoutingManager(operatorUser)
            .GetQueueAsync(department.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(CaseRoutingErrors.QueueForbidden, result.Error);
    }

    [Fact]
    public async Task Claim_OperatorMember_AssignsCaseToSelfAndRemovesItFromQueue()
    {
        var operatorId = Guid.NewGuid();
        var operatorUser = TestUser.Authenticated(MadarRoles.Operator, operatorId);
        var department = ActiveDepartment();
        var shared = SharedFixture.Create([department], [operatorId]);
        shared.Departments.AddMember(department.Id, operatorId);
        var item = CreateCase(Guid.NewGuid(), shared.Clock.UtcNow);
        Assert.True(item.RouteToDepartment(
            department.Id,
            Guid.NewGuid(),
            shared.Clock.UtcNow).IsSuccess);
        shared.Repository.Seed(item);
        var manager = shared.CreateRoutingManager(operatorUser);

        var result = await manager.ClaimAsync(item.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(CaseStatuses.Assigned, result.Value.Status);
        Assert.Equal(operatorId, result.Value.AssignedToUserId);
        Assert.Equal(department.Id, result.Value.DepartmentId);
        var audit = Assert.Single(
            shared.AuditSink.Events,
            entry => entry.Action == "madar.case.claimed");
        Assert.Equal(department.Id.ToString("D"), audit.Attributes["departmentId"]);
        Assert.Equal(operatorId.ToString("D"), audit.Attributes["claimantUserId"]);
        Assert.Equal(2, audit.Attributes.Count);

        var queue = await manager.GetQueueAsync(department.Id);
        Assert.True(queue.IsSuccess);
        Assert.Empty(queue.Value.Cases);
    }

    [Fact]
    public async Task Claim_OperatorOutsideDepartment_IsForbidden()
    {
        var operatorId = Guid.NewGuid();
        var operatorUser = TestUser.Authenticated(MadarRoles.Operator, operatorId);
        var department = ActiveDepartment();
        var shared = SharedFixture.Create([department], [operatorId]);
        var item = CreateCase(Guid.NewGuid(), shared.Clock.UtcNow);
        Assert.True(item.RouteToDepartment(
            department.Id,
            Guid.NewGuid(),
            shared.Clock.UtcNow).IsSuccess);
        shared.Repository.Seed(item);

        var result = await shared.CreateRoutingManager(operatorUser).ClaimAsync(item.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(CaseRoutingErrors.ClaimForbidden, result.Error);
        Assert.Equal(CaseStatuses.New, item.Status);
        Assert.Null(item.AssignedToUserId);
    }

    private static DepartmentDto ActiveDepartment() =>
        new(Guid.NewGuid(), "operations", "العمليات", true);

    private static Case CreateCase(Guid creator, DateTimeOffset createdUtc)
    {
        var result = Case.Create(
            creator,
            "حالة توجيه تشغيلية",
            "وصف كافٍ لاختبار توجيه الحالة إلى القسم واستلامها من قائمة الانتظار.",
            CaseTypes.InternalServiceRequest,
            CasePriorities.Medium,
            createdUtc);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private sealed class SharedFixture
    {
        private SharedFixture(
            FakeCaseRepository repository,
            FakeDepartmentDirectory departments,
            FakeUserDirectory users,
            FakeUnitOfWork unitOfWork,
            CollectingAuditSink auditSink,
            TestClock clock)
        {
            Repository = repository;
            Departments = departments;
            Users = users;
            UnitOfWork = unitOfWork;
            AuditSink = auditSink;
            Clock = clock;
        }

        public FakeCaseRepository Repository { get; }

        public FakeDepartmentDirectory Departments { get; }

        public FakeUserDirectory Users { get; }

        public FakeUnitOfWork UnitOfWork { get; }

        public CollectingAuditSink AuditSink { get; }

        public TestClock Clock { get; }

        public static SharedFixture Create(
            IEnumerable<DepartmentDto> departments,
            IEnumerable<Guid>? operators = null)
        {
            var departmentDirectory = new FakeDepartmentDirectory();
            foreach (var department in departments)
                departmentDirectory.Add(department);

            return new SharedFixture(
                new FakeCaseRepository(),
                departmentDirectory,
                new FakeUserDirectory(operators ?? []),
                new FakeUnitOfWork(),
                new CollectingAuditSink(),
                new TestClock
                {
                    UtcNow = new DateTimeOffset(2026, 8, 8, 14, 0, 0, TimeSpan.Zero)
                });
        }

        public CaseRoutingManager CreateRoutingManager(TestUser user)
        {
            var authorization = new RolePermissionAuthorizationEvaluator(
                user,
                MadarPermissions.CreateRolePermissionMap());
            var auditRecorder = new AuditRecorder(
                AuditSink,
                new TestAuditContextAccessor(user),
                Clock);
            var query = new FakeCaseQueryService(Repository, Clock);

            return new CaseRoutingManager(
                user,
                authorization,
                Repository,
                query,
                Users,
                Departments,
                UnitOfWork,
                auditRecorder,
                Clock);
        }
    }

    private sealed class TestUser : ICurrentUser, IAuthorizationSubject
    {
        private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);

        public bool IsAuthenticated { get; private init; }

        public Guid? UserId { get; private init; }

        public string? Email { get; private init; }

        public bool IsInRole(string role) => _roles.Contains(role);

        public static TestUser Authenticated(string role, Guid? userId = null)
        {
            var user = new TestUser
            {
                IsAuthenticated = true,
                UserId = userId ?? Guid.NewGuid(),
                Email = "routing-test@example.test"
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

    private sealed class FakeCaseQueryService(
        FakeCaseRepository repository,
        TestClock clock) : ICaseQueryService
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
                    .Where(item => item.CreatedByUserId == userId || item.AssignedToUserId == userId)
                    .Select(ToDto)
                    .ToArray());

        public Task<IReadOnlyList<CaseDto>> ListAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CaseDto>>(
                repository.Items.Values.Select(ToDto).ToArray());

        public Task<IReadOnlyList<CaseDto>> ListDepartmentQueueAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CaseDto>>(
                repository.Items.Values
                    .Where(item =>
                        item.DepartmentId == departmentId
                        && item.Status == CaseStatuses.New
                        && item.AssignedToUserId is null)
                    .OrderBy(item => item.CreatedUtc)
                    .Select(ToDto)
                    .ToArray());

        private CaseDto ToDto(Case item) => new(
            item.Id,
            item.CreatedByUserId,
            item.Title,
            item.Description,
            item.CaseType,
            item.Priority,
            item.Status,
            item.DepartmentId,
            item.RoutedUtc,
            item.AssignedToUserId,
            item.CreatedUtc,
            item.UpdatedUtc,
            item.ResolvedUtc,
            item.ClosedUtc,
            item.SlaTargetUtc,
            item.SlaBreachedUtc,
            item.EscalatedUtc,
            item.GetSlaState(clock.UtcNow));
    }

    private sealed class FakeDepartmentDirectory : IDepartmentDirectory
    {
        private readonly Dictionary<Guid, DepartmentDto> _departments = [];
        private readonly HashSet<(Guid DepartmentId, Guid UserId)> _memberships = [];

        public void Add(DepartmentDto department) => _departments[department.Id] = department;

        public void AddMember(Guid departmentId, Guid userId) =>
            _memberships.Add((departmentId, userId));

        public Task<DepartmentDto?> GetAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_departments.GetValueOrDefault(departmentId));

        public Task<IReadOnlyList<DepartmentDto>> ListActiveAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DepartmentDto>>(
                _departments.Values.Where(item => item.IsActive).ToArray());

        public Task<IReadOnlyList<DepartmentDto>> ListForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DepartmentDto>>(
                _departments.Values
                    .Where(item => item.IsActive && _memberships.Contains((item.Id, userId)))
                    .ToArray());

        public Task<bool> IsMemberAsync(
            Guid departmentId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_memberships.Contains((departmentId, userId)));
    }

    private sealed class FakeUserDirectory(IEnumerable<Guid> operators) : IUserDirectory
    {
        private readonly HashSet<Guid> _operators = new(operators);

        public Task<bool> ExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_operators.Contains(userId));

        public Task<bool> IsAssignableOperatorAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_operators.Contains(userId));
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

    private sealed class TestAuditContextAccessor(TestUser user) : IAuditContextAccessor
    {
        public AuditContext Current => new(
            user.UserId?.ToString("D"),
            "routing-test-correlation",
            null,
            "madar-tests");
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
