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
        Assert.Null(result.Value.SlaTargetUtc);
        Assert.Equal(CaseSlaStates.NotApplicable, result.Value.GetSlaState(now));
        Assert.Contains(result.Value.DomainEvents, domainEvent => domainEvent is CaseCreated);
    }

    [Fact]
    public void Create_WithSlaTarget_SnapshotsTargetAndStartsActive()
    {
        var now = Utc(8);
        var target = now.AddHours(2);

        var result = Case.Create(
            Guid.NewGuid(),
            "حالة مرتبطة بمهلة",
            "هذه حالة اختبار لها هدف SLA ثابت عند لحظة الإنشاء.",
            CaseTypes.OperationalIncident,
            CasePriorities.High,
            now,
            target);

        Assert.True(result.IsSuccess);
        Assert.Equal(target, result.Value.SlaTargetUtc);
        Assert.Equal(CaseSlaStates.Active, result.Value.GetSlaState(target));
        Assert.Equal(CaseSlaStates.Breached, result.Value.GetSlaState(target.AddTicks(1)));
    }

    [Fact]
    public void Create_WithNonFutureSlaTarget_IsRejected()
    {
        var now = Utc(8);

        var result = Case.Create(
            Guid.NewGuid(),
            "حالة مرتبطة بمهلة",
            "هذه حالة اختبار تحاول استخدام هدف SLA غير صالح.",
            CaseTypes.OperationalIncident,
            CasePriorities.High,
            now,
            now);

        Assert.True(result.IsFailure);
        Assert.Equal(CaseErrors.InvalidSlaTarget, result.Error);
    }

    [Fact]
    public void EvaluateSla_AfterTarget_PersistsBreachAndEscalationOnlyOnce()
    {
        var now = Utc(8);
        var target = now.AddMinutes(5);
        var @case = CreateValidCase(Guid.NewGuid(), now, target);

        var first = @case.EvaluateSla(target.AddSeconds(1));
        var firstBreachedUtc = @case.SlaBreachedUtc;
        var firstEscalatedUtc = @case.EscalatedUtc;
        var second = @case.EvaluateSla(target.AddMinutes(10));

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(target, firstBreachedUtc);
        Assert.Equal(target.AddSeconds(1), firstEscalatedUtc);
        Assert.Equal(firstBreachedUtc, @case.SlaBreachedUtc);
        Assert.Equal(firstEscalatedUtc, @case.EscalatedUtc);
        Assert.Single(@case.DomainEvents.OfType<CaseSlaBreached>());
        Assert.Equal(CaseSlaStates.Breached, @case.GetSlaState(target.AddMinutes(10)));
    }

    [Fact]
    public void Resolve_AtTarget_MeetsSlaWhileResolveAfterTargetBreaches()
    {
        var target = Utc(10);
        var creator = Guid.NewGuid();
        var operatorUser = Guid.NewGuid();
        var supervisor = Guid.NewGuid();

        var met = CreateValidCase(creator, Utc(9), target);
        Assert.True(met.Assign(operatorUser, supervisor, Utc(9, 1)).IsSuccess);
        Assert.True(met.StartProgress(operatorUser, Utc(9, 2)).IsSuccess);
        Assert.True(met.Resolve(operatorUser, target).IsSuccess);
        Assert.False(met.EvaluateSla(target));
        Assert.Equal(CaseSlaStates.Met, met.GetSlaState(target));

        var breached = CreateValidCase(creator, Utc(9), target);
        Assert.True(breached.Assign(operatorUser, supervisor, Utc(9, 1)).IsSuccess);
        Assert.True(breached.StartProgress(operatorUser, Utc(9, 2)).IsSuccess);
        Assert.True(breached.Resolve(operatorUser, target.AddTicks(1)).IsSuccess);
        Assert.True(breached.EvaluateSla(target.AddTicks(1)));
        Assert.Equal(target, breached.SlaBreachedUtc);
        Assert.Equal(CaseSlaStates.Breached, breached.GetSlaState(target.AddTicks(1)));
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

    private static Case CreateValidCase(
        Guid creator,
        DateTimeOffset createdUtc,
        DateTimeOffset? slaTargetUtc = null)
    {
        var result = Case.Create(
            creator,
            "طلب صلاحية داخلية",
            "طلب منح صلاحية تشغيلية لمستخدم ضمن المسار الأول لمدار.",
            CaseTypes.AccessRequest,
            CasePriorities.Medium,
            createdUtc,
            slaTargetUtc);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static DateTimeOffset Utc(int hour, int minute = 0) =>
        new(2026, 8, 8, hour, minute, 0, TimeSpan.Zero);
}
