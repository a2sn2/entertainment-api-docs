using Madar.Domain.Cases;
using Xunit;

namespace Madar.Tests;

public sealed class CaseTests
{
    [Fact]
    public void Create_WithValidInput_StartsAsNewAndRaisesCreatedEvent()
    {
        var creator = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 8, 8, 0, 0, TimeSpan.Zero);

        var result = Case.Create(
            creator,
            "تعطل تحويل مالي",
            "تعذر على العميل إكمال التحويل ويحتاج إلى متابعة تشغيلية.",
            CaseTypes.OperationalIncident,
            CasePriorities.High,
            now);

        Assert.True(result.IsSuccess);
        Assert.Equal(CaseStatuses.New, result.Value.Status);
        Assert.Equal(creator, result.Value.CreatedByUserId);
        Assert.Null(result.Value.AssignedToUserId);
        Assert.Contains(result.Value.DomainEvents, domainEvent => domainEvent is CaseCreated);
    }

    [Fact]
    public void Lifecycle_ValidSequence_ReachesClosedState()
    {
        var creator = Guid.NewGuid();
        var operatorUser = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 8, 8, 0, 0, TimeSpan.Zero);
        var @case = CreateValidCase(creator, now);

        var assigned = @case.Assign(operatorUser, supervisor, now.AddMinutes(1));
        var started = @case.StartProgress(operatorUser, now.AddMinutes(2));
        var resolved = @case.Resolve(operatorUser, now.AddMinutes(3));
        var closed = @case.Close(supervisor, now.AddMinutes(4));

        Assert.True(assigned.IsSuccess);
        Assert.True(started.IsSuccess);
        Assert.True(resolved.IsSuccess);
        Assert.True(closed.IsSuccess);
        Assert.Equal(CaseStatuses.Closed, @case.Status);
        Assert.Equal(operatorUser, @case.AssignedToUserId);
        Assert.Equal(now.AddMinutes(3), @case.ResolvedUtc);
        Assert.Equal(now.AddMinutes(4), @case.ClosedUtc);
    }

    [Fact]
    public void StartProgress_BeforeAssignment_IsRejected()
    {
        var now = new DateTimeOffset(2026, 8, 8, 8, 0, 0, TimeSpan.Zero);
        var @case = CreateValidCase(Guid.NewGuid(), now);

        var result = @case.StartProgress(Guid.NewGuid(), now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseErrors.InvalidTransition, result.Error);
        Assert.Equal(CaseStatuses.New, @case.Status);
    }

    [Fact]
    public void Create_WithUnsupportedType_IsRejected()
    {
        var result = Case.Create(
            Guid.NewGuid(),
            "طلب تشغيلي",
            "وصف صالح للحالة التشغيلية الجديدة.",
            "unsupported-type",
            CasePriorities.Medium,
            DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CaseErrors.InvalidCaseType, result.Error);
    }

    private static Case CreateValidCase(Guid creator, DateTimeOffset createdUtc)
    {
        var result = Case.Create(
            creator,
            "طلب صلاحية داخلية",
            "طلب منح صلاحية تشغيلية لمستخدم ضمن المسار الأول لمدار.",
            CaseTypes.AccessRequest,
            CasePriorities.Medium,
            createdUtc);

        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
