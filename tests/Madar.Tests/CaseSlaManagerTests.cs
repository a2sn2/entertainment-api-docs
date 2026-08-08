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

public sealed class CaseSlaManagerTests
{
    [Fact]
    public async Task Evaluate_OperatorWithoutPermission_IsForbidden()
    {
        var fixture = CreateFixture(TestCurrentUser.Authenticated(MadarRoles.Operator));

        var result = await fixture.Manager.EvaluateAsync(new EvaluateCaseSlaRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(CaseSlaApplicationErrors.EvaluationForbidden, result.Error);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Evaluate_SupervisorMarksDueCaseAndAuditsExactlyOnce()
    {
        var fixture = CreateFixture(TestCurrentUser.Authenticated(MadarRoles.Supervisor));
        var target = fixture.Clock.UtcNow.AddMinutes(-1);
        var item = CreateCase(target.AddHours(-1), target);
        fixture.Repository.Seed(item);

        var first = await fixture.Manager.EvaluateAsync(new EvaluateCaseSlaRequest());
        var second = await fixture.Manager.EvaluateAsync(new EvaluateCaseSlaRequest());

        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value.EvaluatedCount);
        Assert.Equal(1, first.Value.BreachedCount);
        Assert.False(first.Value.HasMore);
        Assert.Equal(target, item.SlaBreachedUtc);
        Assert.Equal(fixture.Clock.UtcNow, item.EscalatedUtc);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        Assert.Single(
            fixture.AuditSink.Events,
            auditEvent => auditEvent.Action == "madar.case.sla-breached");

        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value.EvaluatedCount);
        Assert.Equal(0, second.Value.BreachedCount);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
        Assert.Single(
            fixture.AuditSink.Events,
            auditEvent => auditEvent.Action == "madar.case.sla-breached");
    }

    [Fact]
    public async Task Evaluate_LimitOneReportsMoreAndLeavesSecondForNextBatch()
    {
        var fixture = CreateFixture(TestCurrentUser.Authenticated(MadarRoles.Administrator));
        var target = fixture.Clock.UtcNow.AddMinutes(-1);
        fixture.Repository.Seed(CreateCase(target.AddHours(-2), target.AddMinutes(-1)));
        fixture.Repository.Seed(CreateCase(target.AddHours(-1), target));

        var first = await fixture.Manager.EvaluateAsync(new EvaluateCaseSlaRequest(1));
        var second = await fixture.Manager.EvaluateAsync(new EvaluateCaseSlaRequest(1));

        Assert.True(first.IsSuccess);
        Assert.Equal(1, first.Value.EvaluatedCount);
        Assert.Equal(1, first.Value.BreachedCount);
        Assert.True(first.Value.HasMore);

        Assert.True(second.IsSuccess);
        Assert.Equal(1, second.Value.EvaluatedCount);
        Assert.Equal(1, second.Value.BreachedCount);
        Assert.False(second.Value.HasMore);
        Assert.Equal(2, fixture.UnitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Evaluate_InvalidBatchLimit_IsRejected(int limit)
    {
        var fixture = CreateFixture(TestCurrentUser.Authenticated(MadarRoles.Supervisor));

        var result = await fixture.Manager.EvaluateAsync(new EvaluateCaseSlaRequest(limit));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseSlaApplicationErrors.InvalidEvaluationLimit, result.Error);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    private static Fixture CreateFixture(TestCurrentUser currentUser)
    {
        var repository = new FakeCaseRepository();
        var queryService = new FakeCaseSlaQueryService(repository);
        var unitOfWork = new FakeUnitOfWork();
        var clock = new TestClock
        {
            UtcNow = new DateTimeOffset(2026, 8, 8, 12, 0, 0, TimeSpan.Zero)
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
            new CaseSlaManager(
                currentUser,
                authorization,
                queryService,
                repository,
                unitOfWork,
                auditRecorder,
                clock),
            repository,
            unitOfWork,
            auditSink,
            clock);
    }

    private static Case CreateCase(
        DateTimeOffset createdUtc,
        DateTimeOffset slaTargetUtc)
    {
        var result = Case.Create(
            Guid.NewGuid(),
            "حالة SLA للاختبار",
            "وصف صالح لحالة تستخدم لاختبار تقييم SLA والتصعيد بشكل حتمي.",
            CaseTypes.OperationalIncident,
            CasePriorities.High,
            createdUtc,
            slaTargetUtc);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private sealed record Fixture(
        CaseSlaManager Manager,
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

        public static TestCurrentUser Authenticated(string role)
        {
            var user = new TestCurrentUser
            {
                IsAuthenticated = true,
                UserId = Guid.NewGuid(),
                Email = "sla-test@example.test"
            };
            user._roles.Add(role);
            return user;
        }
    }

    private sealed class FakeCaseSlaQueryService(FakeCaseRepository repository)
        : ICaseSlaQueryService
    {
        public Task<IReadOnlyList<Guid>> ListDueCaseIdsAsync(
            DateTimeOffset evaluatedUtc,
            int limit,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(
                repository.Items.Values
                    .Where(item =>
                        item.SlaTargetUtc.HasValue
                        && item.SlaTargetUtc.Value < evaluatedUtc
                        && item.SlaBreachedUtc is null
                        && item.ResolvedUtc is null)
                    .OrderBy(item => item.SlaTargetUtc)
                    .ThenBy(item => item.Id)
                    .Take(limit)
                    .Select(item => item.Id)
                    .ToArray());
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
            "madar-sla-test-correlation",
            null,
            "madar-sla-tests");
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
