using FoundationKit.Application.Abstractions;
using FoundationKit.Application.Persistence;
using FoundationKit.Auditing;
using FoundationKit.Authorization;
using Madar.Application.Organization;
using Madar.Application.Security;
using Madar.Contracts.Organization;
using Madar.Contracts.Security;
using Madar.Domain.Organization;
using Xunit;

namespace Madar.Tests;

public sealed class DepartmentAdministrationTests
{
    [Fact]
    public async Task Create_Administrator_NormalizesPersistsAndAuditsDepartment()
    {
        var fixture = CreateFixture(TestUser.Authenticated(MadarRoles.Administrator));

        var result = await fixture.Manager.CreateAsync(
            new CreateDepartmentRequest("  SUPPORT_1 ", "  الدعم التشغيلي  "));

        Assert.True(result.IsSuccess);
        Assert.Equal("support_1", result.Value.Code);
        Assert.Equal("الدعم التشغيلي", result.Value.Name);
        Assert.True(result.Value.IsActive);
        Assert.Equal(result.Value.CreatedUtc, result.Value.UpdatedUtc);
        Assert.Single(fixture.Store.Departments);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        var audit = Assert.Single(
            fixture.AuditSink.Events,
            item => item.Action == "madar.department.created");
        Assert.Equal("support_1", audit.Attributes["code"]);
        Assert.Equal(2, audit.Attributes.Count);
    }

    [Fact]
    public async Task Create_SupervisorWithoutManagementPermission_IsForbidden()
    {
        var fixture = CreateFixture(TestUser.Authenticated(MadarRoles.Supervisor));

        var result = await fixture.Manager.CreateAsync(
            new CreateDepartmentRequest("support", "الدعم"));

        Assert.True(result.IsFailure);
        Assert.Equal(DepartmentAdministrationErrors.Forbidden, result.Error);
        Assert.Empty(fixture.Store.Departments);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_DuplicateStableCode_IsRejectedDeterministically()
    {
        var fixture = CreateFixture(TestUser.Authenticated(MadarRoles.Administrator));
        fixture.Store.Seed(CreateDepartment("support", "الدعم", fixture.Clock.UtcNow));

        var result = await fixture.Manager.CreateAsync(
            new CreateDepartmentRequest("SUPPORT", "الدعم الثاني"));

        Assert.True(result.IsFailure);
        Assert.Equal(DepartmentAdministrationErrors.CodeAlreadyExists, result.Error);
        Assert.Single(fixture.Store.Departments);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_DeactivateDepartmentWithOpenCases_IsBlocked()
    {
        var fixture = CreateFixture(TestUser.Authenticated(MadarRoles.Administrator));
        var department = CreateDepartment("operations", "العمليات", fixture.Clock.UtcNow);
        fixture.Store.Seed(department);
        fixture.Store.OpenDepartments.Add(department.Id);

        var result = await fixture.Manager.UpdateAsync(
            department.Id,
            new UpdateDepartmentRequest("العمليات", false));

        Assert.True(result.IsFailure);
        Assert.Equal(DepartmentAdministrationErrors.DepartmentHasOpenCases, result.Error);
        Assert.True(department.IsActive);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_RenameAndDeactivateWithoutOpenCases_PersistsAudit()
    {
        var fixture = CreateFixture(TestUser.Authenticated(MadarRoles.Administrator));
        var department = CreateDepartment("operations", "العمليات", fixture.Clock.UtcNow);
        fixture.Store.Seed(department);
        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddMinutes(10);

        var result = await fixture.Manager.UpdateAsync(
            department.Id,
            new UpdateDepartmentRequest("العمليات المركزية", false));

        Assert.True(result.IsSuccess);
        Assert.Equal("العمليات المركزية", result.Value.Name);
        Assert.False(result.Value.IsActive);
        Assert.Equal(fixture.Clock.UtcNow, result.Value.UpdatedUtc);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        var audit = Assert.Single(
            fixture.AuditSink.Events,
            item => item.Action == "madar.department.updated");
        Assert.Equal("True", audit.Attributes["previousActive"]);
        Assert.Equal("False", audit.Attributes["active"]);
        Assert.Equal(3, audit.Attributes.Count);
    }

    [Fact]
    public async Task AddMember_UserWithoutOperatorRole_IsRejected()
    {
        var fixture = CreateFixture(TestUser.Authenticated(MadarRoles.Administrator));
        var department = CreateDepartment("operations", "العمليات", fixture.Clock.UtcNow);
        fixture.Store.Seed(department);

        var result = await fixture.Manager.AddMemberAsync(
            department.Id,
            new AddDepartmentMemberRequest(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(DepartmentAdministrationErrors.MemberMustBeOperator, result.Error);
        Assert.Empty(fixture.Store.Memberships);
    }

    [Fact]
    public async Task AddMember_Operator_PersistsMembershipAndBoundedAudit()
    {
        var operatorId = Guid.NewGuid();
        var fixture = CreateFixture(
            TestUser.Authenticated(MadarRoles.Administrator),
            [operatorId]);
        var department = CreateDepartment("operations", "العمليات", fixture.Clock.UtcNow);
        fixture.Store.Seed(department);

        var result = await fixture.Manager.AddMemberAsync(
            department.Id,
            new AddDepartmentMemberRequest(operatorId));

        Assert.True(result.IsSuccess);
        Assert.Equal(operatorId, result.Value.UserId);
        Assert.Contains((department.Id, operatorId), fixture.Store.Memberships.Keys);
        var audit = Assert.Single(
            fixture.AuditSink.Events,
            item => item.Action == "madar.department.member-added");
        Assert.Equal(department.Id.ToString("D"), audit.Attributes["departmentId"]);
        Assert.Equal(operatorId.ToString("D"), audit.Attributes["userId"]);
        Assert.Equal(2, audit.Attributes.Count);
    }

    [Fact]
    public async Task AddMember_DuplicateMembership_IsRejectedBeforePersistence()
    {
        var operatorId = Guid.NewGuid();
        var fixture = CreateFixture(
            TestUser.Authenticated(MadarRoles.Administrator),
            [operatorId]);
        var department = CreateDepartment("operations", "العمليات", fixture.Clock.UtcNow);
        fixture.Store.Seed(department);
        fixture.Store.SeedMembership(department.Id, operatorId, fixture.Clock.UtcNow);

        var result = await fixture.Manager.AddMemberAsync(
            department.Id,
            new AddDepartmentMemberRequest(operatorId));

        Assert.True(result.IsFailure);
        Assert.Equal(DepartmentAdministrationErrors.MembershipAlreadyExists, result.Error);
        Assert.Single(fixture.Store.Memberships);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task RemoveMember_WithOpenAssignment_IsBlocked()
    {
        var operatorId = Guid.NewGuid();
        var fixture = CreateFixture(
            TestUser.Authenticated(MadarRoles.Administrator),
            [operatorId]);
        var department = CreateDepartment("operations", "العمليات", fixture.Clock.UtcNow);
        fixture.Store.Seed(department);
        fixture.Store.SeedMembership(department.Id, operatorId, fixture.Clock.UtcNow);
        fixture.Store.OpenAssignments.Add((department.Id, operatorId));

        var result = await fixture.Manager.RemoveMemberAsync(
            department.Id,
            operatorId);

        Assert.True(result.IsFailure);
        Assert.Equal(DepartmentAdministrationErrors.MemberHasOpenAssignments, result.Error);
        Assert.Single(fixture.Store.Memberships);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task RemoveMember_WithoutOpenAssignment_RemovesAndAudits()
    {
        var operatorId = Guid.NewGuid();
        var fixture = CreateFixture(
            TestUser.Authenticated(MadarRoles.Administrator),
            [operatorId]);
        var department = CreateDepartment("operations", "العمليات", fixture.Clock.UtcNow);
        fixture.Store.Seed(department);
        fixture.Store.SeedMembership(department.Id, operatorId, fixture.Clock.UtcNow);

        var result = await fixture.Manager.RemoveMemberAsync(
            department.Id,
            operatorId);

        Assert.True(result.IsSuccess);
        Assert.Empty(fixture.Store.Memberships);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        var audit = Assert.Single(
            fixture.AuditSink.Events,
            item => item.Action == "madar.department.member-removed");
        Assert.Equal(2, audit.Attributes.Count);
    }

    [Fact]
    public void Department_Update_InvalidName_DoesNotMutateState()
    {
        var createdUtc = Utc(9);
        var department = CreateDepartment("operations", "العمليات", createdUtc);

        var result = department.Update(" ", false, Utc(10));

        Assert.True(result.IsFailure);
        Assert.Equal(DepartmentErrors.InvalidName, result.Error);
        Assert.Equal("العمليات", department.Name);
        Assert.True(department.IsActive);
        Assert.Equal(createdUtc, department.UpdatedUtc);
    }

    private static Fixture CreateFixture(
        TestUser currentUser,
        IEnumerable<Guid>? operatorIds = null)
    {
        var store = new FakeDepartmentAdministrationStore();
        var users = new FakeUserDirectory(operatorIds ?? []);
        var unitOfWork = new FakeUnitOfWork();
        var auditSink = new CollectingAuditSink();
        var clock = new TestClock { UtcNow = Utc(9) };
        var authorization = new RolePermissionAuthorizationEvaluator(
            currentUser,
            MadarPermissions.CreateRolePermissionMap());
        var auditRecorder = new AuditRecorder(
            auditSink,
            new TestAuditContextAccessor(currentUser),
            clock);

        return new Fixture(
            new DepartmentAdministrationManager(
                currentUser,
                authorization,
                store,
                users,
                unitOfWork,
                auditRecorder,
                clock),
            store,
            unitOfWork,
            auditSink,
            clock);
    }

    private static Department CreateDepartment(
        string code,
        string name,
        DateTimeOffset createdUtc)
    {
        var result = Department.Create(code, name, createdUtc);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static DateTimeOffset Utc(int hour) =>
        new(2026, 8, 8, hour, 0, 0, TimeSpan.Zero);

    private sealed record Fixture(
        DepartmentAdministrationManager Manager,
        FakeDepartmentAdministrationStore Store,
        FakeUnitOfWork UnitOfWork,
        CollectingAuditSink AuditSink,
        TestClock Clock);

    private sealed class TestUser : ICurrentUser, IAuthorizationSubject
    {
        private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);

        public bool IsAuthenticated { get; private init; }

        public Guid? UserId { get; private init; }

        public string? Email { get; private init; }

        public bool IsInRole(string role) => _roles.Contains(role);

        public static TestUser Authenticated(string role)
        {
            var user = new TestUser
            {
                IsAuthenticated = true,
                UserId = Guid.NewGuid(),
                Email = "department-admin@example.test"
            };
            user._roles.Add(role);
            return user;
        }
    }

    private sealed class FakeDepartmentAdministrationStore
        : IDepartmentAdministrationStore
    {
        public Dictionary<Guid, Department> Departments { get; } = [];

        public Dictionary<(Guid DepartmentId, Guid UserId), DepartmentMembership> Memberships { get; } = [];

        public HashSet<Guid> OpenDepartments { get; } = [];

        public HashSet<(Guid DepartmentId, Guid UserId)> OpenAssignments { get; } = [];

        public void Seed(Department department) =>
            Departments[department.Id] = department;

        public void SeedMembership(
            Guid departmentId,
            Guid userId,
            DateTimeOffset joinedUtc)
        {
            var result = DepartmentMembership.Create(
                departmentId,
                userId,
                joinedUtc);
            Assert.True(result.IsSuccess);
            Memberships[(departmentId, userId)] = result.Value;
        }

        public Task<IReadOnlyList<DepartmentAdminDto>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DepartmentAdminDto>>(
                Departments.Values.Select(ToDto).ToArray());

        public Task<Department?> FindAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Departments.GetValueOrDefault(departmentId));

        public Task<bool> CodeExistsAsync(
            string code,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Departments.Values.Any(item =>
                    string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(
            Department department,
            CancellationToken cancellationToken = default)
        {
            Departments[department.Id] = department;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DepartmentMemberDto>> ListMembersAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DepartmentMemberDto>>(
                Memberships.Values
                    .Where(item => item.DepartmentId == departmentId)
                    .Select(ToMemberDto)
                    .ToArray());

        public Task<DepartmentMemberDto?> GetMemberAsync(
            Guid departmentId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Memberships.TryGetValue((departmentId, userId), out var membership)
                    ? ToMemberDto(membership)
                    : null);

        public Task<DepartmentMembership?> FindMembershipAsync(
            Guid departmentId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Memberships.GetValueOrDefault((departmentId, userId)));

        public Task AddMembershipAsync(
            DepartmentMembership membership,
            CancellationToken cancellationToken = default)
        {
            Memberships[(membership.DepartmentId, membership.UserId)] = membership;
            return Task.CompletedTask;
        }

        public void RemoveMembership(DepartmentMembership membership) =>
            Memberships.Remove((membership.DepartmentId, membership.UserId));

        public Task<bool> HasOpenCasesAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OpenDepartments.Contains(departmentId));

        public Task<bool> HasOpenAssignedCasesAsync(
            Guid departmentId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(OpenAssignments.Contains((departmentId, userId)));

        private static DepartmentAdminDto ToDto(Department department) =>
            new(
                department.Id,
                department.Code,
                department.Name,
                department.IsActive,
                department.CreatedUtc,
                department.UpdatedUtc);

        private static DepartmentMemberDto ToMemberDto(
            DepartmentMembership membership) =>
            new(
                membership.UserId,
                $"Operator {membership.UserId:D}",
                "operator@example.test",
                membership.JoinedUtc);
    }

    private sealed class FakeUserDirectory(IEnumerable<Guid> operatorIds)
        : IUserDirectory
    {
        private readonly HashSet<Guid> _operators = new(operatorIds);

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

    private sealed class TestAuditContextAccessor(TestUser currentUser)
        : IAuditContextAccessor
    {
        public AuditContext Current => new(
            currentUser.UserId?.ToString("D"),
            "madar-department-admin-test",
            null,
            "madar-tests");
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
