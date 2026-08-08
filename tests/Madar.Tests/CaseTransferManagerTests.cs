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

public sealed class CaseTransferManagerTests
{
    [Fact]
    public async Task Transfer_Supervisor_MovesActiveCaseToTargetQueueWithBoundedAudit()
    {
        var supervisor = TestUser.Authenticated(MadarRoles.Supervisor);
        var firstOperator = Guid.NewGuid();
        var source = Department("operations", "العمليات");
        var target = Department("support", "الدعم");
        var fixture = Fixture.Create([source, target], [firstOperator]);
        var item = CreateCase(fixture.Clock.UtcNow);
        Assert.True(item.RouteToDepartment(source.Id, supervisor.UserId!.Value, fixture.Clock.UtcNow).IsSuccess);
        Assert.True(item.Assign(firstOperator, supervisor.UserId.Value, fixture.Clock.UtcNow).IsSuccess);
        Assert.True(item.StartProgress(firstOperator, fixture.Clock.UtcNow).IsSuccess);
        fixture.Repository.Seed(item);

        var result = await fixture.CreateManager(supervisor).TransferAsync(
            item.Id,
            new TransferCaseRequest(target.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(target.Id, result.Value.DepartmentId);
        Assert.Equal(CaseStatuses.New, result.Value.Status);
        Assert.Null(result.Value.AssignedToUserId);
        var audit = Assert.Single(
            fixture.AuditSink.Events,
            entry => entry.Action == "madar.case.transferred");
        Assert.Equal(source.Id.ToString("D"), audit.Attributes["fromDepartmentId"]);
        Assert.Equal(target.Id.ToString("D"), audit.Attributes["toDepartmentId"]);
        Assert.Equal(firstOperator.ToString("D"), audit.Attributes["previousAssigneeUserId"]);
        Assert.Equal(CaseStatuses.InProgress, audit.Attributes["previousStatus"]);
        Assert.Equal(4, audit.Attributes.Count);
    }

    [Fact]
    public async Task Transfer_Operator_IsForbidden()
    {
        var operatorUser = TestUser.Authenticated(MadarRoles.Operator);
        var source = Department("operations", "العمليات");
        var target = Department("support", "الدعم");
        var fixture = Fixture.Create([source, target], [operatorUser.UserId!.Value]);
        fixture.Departments.AddMember(source.Id, operatorUser.UserId.Value);
        var item = CreateCase(fixture.Clock.UtcNow);
        Assert.True(item.RouteToDepartment(source.Id, Guid.NewGuid(), fixture.Clock.UtcNow).IsSuccess);
        fixture.Repository.Seed(item);

        var result = await fixture.CreateManager(operatorUser).TransferAsync(
            item.Id,
            new TransferCaseRequest(target.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseRoutingErrors.TransferForbidden, result.Error);
        Assert.Equal(source.Id, item.DepartmentId);
    }

    [Fact]
    public async Task Reassign_Supervisor_PreservesProgressAndNotifiesOnlyAfterCommit()
    {
        var supervisor = TestUser.Authenticated(MadarRoles.Supervisor);
        var firstOperator = Guid.NewGuid();
        var secondOperator = Guid.NewGuid();
        var department = Department("operations", "العمليات");
        var fixture = Fixture.Create(
            [department],
            [firstOperator, secondOperator]);
        fixture.Departments.AddMember(department.Id, firstOperator);
        fixture.Departments.AddMember(department.Id, secondOperator);
        var item = CreateCase(fixture.Clock.UtcNow);
        Assert.True(item.RouteToDepartment(department.Id, supervisor.UserId!.Value, fixture.Clock.UtcNow).IsSuccess);
        Assert.True(item.Assign(firstOperator, supervisor.UserId.Value, fixture.Clock.UtcNow).IsSuccess);
        Assert.True(item.StartProgress(firstOperator, fixture.Clock.UtcNow).IsSuccess);
        fixture.Repository.Seed(item);
        var notifications = new TrackingNotificationCoordinator(fixture.UnitOfWork);

        var result = await fixture.CreateManager(supervisor, notifications).ReassignAsync(
            item.Id,
            new ReassignCaseRequest(secondOperator));

        Assert.True(result.IsSuccess);
        Assert.Equal(secondOperator, result.Value.AssignedToUserId);
        Assert.Equal(CaseStatuses.InProgress, result.Value.Status);
        Assert.True(notifications.Notified);
        Assert.True(notifications.SaveCountWhenNotified >= 1);
        Assert.Equal(secondOperator, notifications.TargetUserId);
        var audit = Assert.Single(
            fixture.AuditSink.Events,
            entry => entry.Action == "madar.case.reassigned");
        Assert.Equal(firstOperator.ToString("D"), audit.Attributes["previousAssigneeUserId"]);
        Assert.Equal(secondOperator.ToString("D"), audit.Attributes["assigneeUserId"]);
        Assert.Equal(department.Id.ToString("D"), audit.Attributes["departmentId"]);
        Assert.Equal(CaseStatuses.InProgress, audit.Attributes["status"]);
        Assert.Equal(4, audit.Attributes.Count);
    }

    [Fact]
    public async Task Reassign_ToOperatorOutsideDepartment_IsRejected()
    {
        var supervisor = TestUser.Authenticated(MadarRoles.Supervisor);
        var firstOperator = Guid.NewGuid();
        var outsideOperator = Guid.NewGuid();
        var department = Department("operations", "العمليات");
        var fixture = Fixture.Create(
            [department],
            [firstOperator, outsideOperator]);
        fixture.Departments.AddMember(department.Id, firstOperator);
        var item = CreateCase(fixture.Clock.UtcNow);
        Assert.True(item.RouteToDepartment(department.Id, supervisor.UserId!.Value, fixture.Clock.UtcNow).IsSuccess);
        Assert.True(item.Assign(firstOperator, supervisor.UserId.Value, fixture.Clock.UtcNow).IsSuccess);
        fixture.Repository.Seed(item);

        var result = await fixture.CreateManager(supervisor).ReassignAsync(
            item.Id,
            new ReassignCaseRequest(outsideOperator));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseRoutingErrors.AssigneeOutsideDepartment, result.Error);
        Assert.Equal(firstOperator, item.AssignedToUserId);
    }

    [Fact]
    public async Task Reassign_Operator_IsForbidden()
    {
        var firstOperator = Guid.NewGuid();
        var secondOperator = Guid.NewGuid();
        var operatorUser = TestUser.Authenticated(MadarRoles.Operator, firstOperator);
        var fixture = Fixture.Create([], [firstOperator, secondOperator]);
        var item = CreateCase(fixture.Clock.UtcNow);
        Assert.True(item.Assign(firstOperator, Guid.NewGuid(), fixture.Clock.UtcNow).IsSuccess);
        fixture.Repository.Seed(item);

        var result = await fixture.CreateManager(operatorUser).ReassignAsync(
            item.Id,
            new ReassignCaseRequest(secondOperator));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseRoutingErrors.ReassignmentForbidden, result.Error);
        Assert.Equal(firstOperator, item.AssignedToUserId);
    }

    private static DepartmentDto Department(string code, string name) =>
        new(Guid.NewGuid(), code, name, true);

    private static Case CreateCase(DateTimeOffset now)
    {
        var created = Case.Create(
            Guid.NewGuid(),
            "حالة نقل وإعادة إسناد",
            "وصف كافٍ للتحقق من النقل وإعادة الإسناد على مستوى التطبيق مع الصلاحيات والتدقيق.",
            CaseTypes.InternalServiceRequest,
            CasePriorities.High,
            now,
            now.AddHours(2));
        Assert.True(created.IsSuccess);
        return created.Value;
    }

    private sealed class Fixture
    {
        private Fixture(
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

        public static Fixture Create(
            IEnumerable<DepartmentDto> departments,
            IEnumerable<Guid> operators)
        {
            var directory = new FakeDepartmentDirectory();
            foreach (var department in departments)
                directory.Add(department);

            return new Fixture(
                new FakeCaseRepository(),
                directory,
                new FakeUserDirectory(operators),
                new FakeUnitOfWork(),
                new CollectingAuditSink(),
                new TestClock
                {
                    UtcNow = new DateTimeOffset(2026, 8, 8, 17, 0, 0, TimeSpan.Zero)
                });
        }

        public CaseRoutingManager CreateManager(
            TestUser user,
            ICaseNotificationCoordinator? notificationCoordinator = null)
        {
            var authorization = new RolePermissionAuthorizationEvaluator(
                user,
                MadarPermissions.CreateRolePermissionMap());
            var audit = new AuditRecorder(
                AuditSink,
                new TestAuditContextAccessor(user),
                Clock);

            return new CaseRoutingManager(
                user,
                authorization,
                Repository,
                new FakeCaseQueryService(Repository, Clock),
                Users,
                Departments,
                UnitOfWork,
                audit,
                Clock,
                notificationCoordinator);
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
            Task.FromResult<IReadOnlyList<CaseDto>>([]);

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

        public Task<string?> GetNotificationDestinationAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
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

    private sealed class TrackingNotificationCoordinator(FakeUnitOfWork unitOfWork)
        : ICaseNotificationCoordinator
    {
        public bool Notified { get; private set; }
        public int SaveCountWhenNotified { get; private set; }
        public Guid? TargetUserId { get; private set; }

        public Task NotifyAssignmentAsync(
            Guid caseId,
            Guid assigneeUserId,
            CancellationToken cancellationToken = default)
        {
            Notified = true;
            TargetUserId = assigneeUserId;
            SaveCountWhenNotified = unitOfWork.SaveCount;
            return Task.CompletedTask;
        }

        public Task NotifyApprovalDecisionAsync(
            Guid caseId,
            Guid requesterUserId,
            string decision,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyResolutionAsync(
            Guid caseId,
            Guid creatorUserId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
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
                Email = "transfer-test@example.test"
            };
            user._roles.Add(role);
            return user;
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
            "transfer-test-correlation",
            null,
            "madar-tests");
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
