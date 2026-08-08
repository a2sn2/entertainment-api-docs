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

public sealed class CaseCommentTests
{
    [Fact]
    public void Create_TrimsBoundedBody()
    {
        var result = CaseComment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  متابعة تشغيلية واضحة  ",
            Utc(10));

        Assert.True(result.IsSuccess);
        Assert.Equal("متابعة تشغيلية واضحة", result.Value.Body);
        Assert.Equal(Utc(10), result.Value.CreatedUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyBody_IsRejected(string body)
    {
        var result = CaseComment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            body,
            Utc(10));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseCommentErrors.InvalidBody, result.Error);
    }

    [Fact]
    public void Create_OverMaximumBody_IsRejected()
    {
        var result = CaseComment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('x', 2001),
            Utc(10));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseCommentErrors.InvalidBody, result.Error);
    }

    [Fact]
    public async Task Add_CaseCreator_PersistsAndAuditsWithoutBody()
    {
        var creator = TestCurrentUser.Authenticated(MadarRoles.Requester);
        var fixture = CreateFixture(creator);
        var item = CreateCase(creator.UserId!.Value);
        fixture.Cases.Seed(item);
        const string sensitiveBody = "تفاصيل داخلية لا يجب نسخها إلى سجل التدقيق";

        var result = await fixture.Manager.AddAsync(
            item.Id,
            new AddCaseCommentRequest(sensitiveBody));

        Assert.True(result.IsSuccess);
        Assert.Equal(sensitiveBody, result.Value.Body);
        Assert.Equal(creator.UserId, result.Value.AuthorUserId);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);

        var audit = Assert.Single(
            fixture.AuditSink.Events,
            entry => entry.Action == "madar.case.comment-added");
        Assert.Equal(item.Id.ToString("D"), audit.SubjectId);
        Assert.Contains("commentId", audit.Attributes.Keys);
        Assert.DoesNotContain(
            audit.Attributes.Values,
            value => value.Contains("تفاصيل داخلية", StringComparison.Ordinal));
        Assert.DoesNotContain("body", audit.Attributes.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task List_AssignedOperator_CanReadComments()
    {
        var operatorId = Guid.NewGuid();
        var currentUser = TestCurrentUser.Authenticated(
            MadarRoles.Operator,
            operatorId);
        var fixture = CreateFixture(currentUser);
        var item = CreateCase(Guid.NewGuid());
        Assert.True(item.Assign(
            operatorId,
            Guid.NewGuid(),
            Utc(9, 1)).IsSuccess);
        fixture.Cases.Seed(item);
        fixture.Comments.Seed(CreateComment(item.Id, Guid.NewGuid(), "تعليق موجود"));

        var result = await fixture.Manager.ListAsync(item.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("تعليق موجود", result.Value[0].Body);
    }

    [Fact]
    public async Task Add_UnrelatedOperator_IsMaskedAsNotFound()
    {
        var currentUser = TestCurrentUser.Authenticated(MadarRoles.Operator);
        var fixture = CreateFixture(currentUser);
        var item = CreateCase(Guid.NewGuid());
        fixture.Cases.Seed(item);

        var result = await fixture.Manager.AddAsync(
            item.Id,
            new AddCaseCommentRequest("تعليق يجب ألا يُسمح به"));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseApplicationErrors.CaseNotFound, result.Error);
        Assert.Empty(fixture.Comments.Items);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Add_SupervisorWithReadAll_CanComment()
    {
        var supervisor = TestCurrentUser.Authenticated(MadarRoles.Supervisor);
        var fixture = CreateFixture(supervisor);
        var item = CreateCase(Guid.NewGuid());
        fixture.Cases.Seed(item);

        var result = await fixture.Manager.AddAsync(
            item.Id,
            new AddCaseCommentRequest("ملاحظة إشرافية"));

        Assert.True(result.IsSuccess);
        Assert.Equal(supervisor.UserId, result.Value.AuthorUserId);
    }

    private static Fixture CreateFixture(TestCurrentUser currentUser)
    {
        var cases = new FakeCaseRepository();
        var comments = new FakeCommentStore();
        var unitOfWork = new FakeUnitOfWork();
        var clock = new TestClock { UtcNow = Utc(10) };
        var auditSink = new CollectingAuditSink();
        var authorization = new RolePermissionAuthorizationEvaluator(
            currentUser,
            MadarPermissions.CreateRolePermissionMap());
        var recorder = new AuditRecorder(
            auditSink,
            new TestAuditContextAccessor(currentUser),
            clock);

        return new Fixture(
            new CaseCommentManager(
                currentUser,
                authorization,
                cases,
                comments,
                comments,
                unitOfWork,
                recorder,
                clock),
            cases,
            comments,
            unitOfWork,
            auditSink);
    }

    private static Case CreateCase(Guid creator)
    {
        var result = Case.Create(
            creator,
            "حالة للتعليقات",
            "وصف صالح لحالة تستخدم في اختبارات تعليقات التعاون داخل مدار.",
            CaseTypes.InternalServiceRequest,
            CasePriorities.Medium,
            Utc(9));
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static CaseComment CreateComment(
        Guid caseId,
        Guid author,
        string body)
    {
        var result = CaseComment.Create(caseId, author, body, Utc(9, 30));
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static DateTimeOffset Utc(int hour, int minute = 0) =>
        new(2026, 8, 8, hour, minute, 0, TimeSpan.Zero);

    private sealed record Fixture(
        CaseCommentManager Manager,
        FakeCaseRepository Cases,
        FakeCommentStore Comments,
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
                Email = "comment-test@example.test"
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

        public Task<Case?> FirstOrDefaultAsync(ISpecification<Case> specification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Case>> ListAsync(ISpecification<Case>? specification = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Case>>(Items.Values.ToArray());

        public Task<int> CountAsync(ISpecification<Case>? specification = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count);

        public Task AddAsync(Case entity, CancellationToken cancellationToken = default)
        {
            Items[entity.Id] = entity;
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<Case> entities, CancellationToken cancellationToken = default)
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

    private sealed class FakeCommentStore : ICaseCommentStore, ICaseCommentQueryService
    {
        public Dictionary<Guid, CaseComment> Items { get; } = [];

        public void Seed(CaseComment comment) => Items[comment.Id] = comment;

        public Task AddAsync(CaseComment comment, CancellationToken cancellationToken = default)
        {
            Items[comment.Id] = comment;
            return Task.CompletedTask;
        }

        public Task<CaseCommentDto?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Items.TryGetValue(commentId, out var comment)
                    ? ToDto(comment)
                    : null);

        public Task<IReadOnlyList<CaseCommentDto>> ListForCaseAsync(Guid caseId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CaseCommentDto>>(
                Items.Values
                    .Where(item => item.CaseId == caseId)
                    .OrderBy(item => item.CreatedUtc)
                    .ThenBy(item => item.Id)
                    .Select(ToDto)
                    .ToArray());

        private static CaseCommentDto ToDto(CaseComment comment) =>
            new(
                comment.Id,
                comment.CaseId,
                comment.AuthorUserId,
                "كاتب الاختبار",
                comment.Body,
                comment.CreatedUtc);
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
        public ValueTask WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestAuditContextAccessor(TestCurrentUser currentUser) : IAuditContextAccessor
    {
        public AuditContext Current => new(
            currentUser.UserId?.ToString("D"),
            "madar-comment-test-correlation",
            null,
            "madar-comment-tests");
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
