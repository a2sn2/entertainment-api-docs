using Madar.Domain.Cases;
using Xunit;

namespace Madar.Tests;

public sealed class CaseTransferDomainTests
{
    [Fact]
    public void Transfer_InProgressCase_MovesToNewQueueAndPreservesSlaEvidence()
    {
        var now = Utc(8);
        var sourceDepartment = Guid.NewGuid();
        var targetDepartment = Guid.NewGuid();
        var operatorUser = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        var @case = CreateCase(now, now.AddHours(2));

        Assert.True(@case.RouteToDepartment(sourceDepartment, supervisor, now.AddMinutes(1)).IsSuccess);
        Assert.True(@case.Assign(operatorUser, supervisor, now.AddMinutes(2)).IsSuccess);
        Assert.True(@case.StartProgress(operatorUser, now.AddMinutes(3)).IsSuccess);

        var result = @case.TransferToDepartment(
            targetDepartment,
            supervisor,
            now.AddMinutes(4));

        Assert.True(result.IsSuccess);
        Assert.Equal(targetDepartment, @case.DepartmentId);
        Assert.Equal(CaseStatuses.New, @case.Status);
        Assert.Null(@case.AssignedToUserId);
        Assert.Equal(now.AddMinutes(4), @case.RoutedUtc);
        Assert.Equal(now.AddHours(2), @case.SlaTargetUtc);
        Assert.Null(@case.SlaBreachedUtc);
        var transferred = Assert.Single(@case.DomainEvents.OfType<CaseTransferred>());
        Assert.Equal(sourceDepartment, transferred.FromDepartmentId);
        Assert.Equal(targetDepartment, transferred.ToDepartmentId);
        Assert.Equal(operatorUser, transferred.PreviousAssigneeUserId);
        Assert.Equal(CaseStatuses.InProgress, transferred.PreviousStatus);
    }

    [Fact]
    public void Transfer_ToSameDepartment_IsRejectedWithoutMutation()
    {
        var now = Utc(8);
        var department = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        var @case = CreateCase(now);
        Assert.True(@case.RouteToDepartment(department, supervisor, now.AddMinutes(1)).IsSuccess);

        var result = @case.TransferToDepartment(
            department,
            supervisor,
            now.AddMinutes(2));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseErrors.SameDepartment, result.Error);
        Assert.Equal(department, @case.DepartmentId);
        Assert.Equal(CaseStatuses.New, @case.Status);
    }

    [Fact]
    public void Transfer_ResolvedCase_IsRejected()
    {
        var now = Utc(8);
        var sourceDepartment = Guid.NewGuid();
        var targetDepartment = Guid.NewGuid();
        var operatorUser = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        var @case = CreateCase(now);
        Assert.True(@case.RouteToDepartment(sourceDepartment, supervisor, now.AddMinutes(1)).IsSuccess);
        Assert.True(@case.Assign(operatorUser, supervisor, now.AddMinutes(2)).IsSuccess);
        Assert.True(@case.StartProgress(operatorUser, now.AddMinutes(3)).IsSuccess);
        Assert.True(@case.Resolve(operatorUser, now.AddMinutes(4)).IsSuccess);

        var result = @case.TransferToDepartment(
            targetDepartment,
            supervisor,
            now.AddMinutes(5));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseErrors.InvalidTransferState, result.Error);
        Assert.Equal(sourceDepartment, @case.DepartmentId);
        Assert.Equal(CaseStatuses.Resolved, @case.Status);
    }

    [Fact]
    public void Reassign_InProgressCase_PreservesLifecycleAndSla()
    {
        var now = Utc(8);
        var department = Guid.NewGuid();
        var firstOperator = Guid.NewGuid();
        var secondOperator = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        var @case = CreateCase(now, now.AddHours(2));
        Assert.True(@case.RouteToDepartment(department, supervisor, now.AddMinutes(1)).IsSuccess);
        Assert.True(@case.Assign(firstOperator, supervisor, now.AddMinutes(2)).IsSuccess);
        Assert.True(@case.StartProgress(firstOperator, now.AddMinutes(3)).IsSuccess);

        var result = @case.Reassign(
            secondOperator,
            supervisor,
            now.AddMinutes(4));

        Assert.True(result.IsSuccess);
        Assert.Equal(secondOperator, @case.AssignedToUserId);
        Assert.Equal(CaseStatuses.InProgress, @case.Status);
        Assert.Equal(department, @case.DepartmentId);
        Assert.Equal(now.AddHours(2), @case.SlaTargetUtc);
        var reassigned = Assert.Single(@case.DomainEvents.OfType<CaseReassigned>());
        Assert.Equal(firstOperator, reassigned.PreviousAssigneeUserId);
        Assert.Equal(secondOperator, reassigned.AssigneeUserId);
        Assert.Equal(CaseStatuses.InProgress, reassigned.Status);
    }

    [Fact]
    public void Reassign_ToSameOperator_IsRejected()
    {
        var now = Utc(8);
        var operatorUser = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        var @case = CreateCase(now);
        Assert.True(@case.Assign(operatorUser, supervisor, now.AddMinutes(1)).IsSuccess);

        var result = @case.Reassign(
            operatorUser,
            supervisor,
            now.AddMinutes(2));

        Assert.True(result.IsFailure);
        Assert.Equal(CaseErrors.SameAssignee, result.Error);
        Assert.Equal(operatorUser, @case.AssignedToUserId);
        Assert.Equal(CaseStatuses.Assigned, @case.Status);
    }

    private static Case CreateCase(
        DateTimeOffset createdUtc,
        DateTimeOffset? slaTargetUtc = null)
    {
        var result = Case.Create(
            Guid.NewGuid(),
            "حالة نقل تشغيلية",
            "وصف كافٍ لاختبار نقل الحالة وإعادة إسنادها مع الحفاظ على السجل التشغيلي.",
            CaseTypes.InternalServiceRequest,
            CasePriorities.High,
            createdUtc,
            slaTargetUtc);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static DateTimeOffset Utc(int hour, int minute = 0) =>
        new(2026, 8, 8, hour, minute, 0, TimeSpan.Zero);
}
