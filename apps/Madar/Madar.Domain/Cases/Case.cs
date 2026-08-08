using FoundationKit.Application.Results;
using FoundationKit.Domain.Events;
using FoundationKit.Domain.Primitives;

namespace Madar.Domain.Cases;

public static class CaseTypes
{
    public const string CustomerComplaint = "customer-complaint";
    public const string OperationalIncident = "operational-incident";
    public const string InternalServiceRequest = "internal-service-request";
    public const string AccessRequest = "access-request";
    public const string ComplianceCase = "compliance-case";
    public const string TechnicalEscalation = "technical-escalation";
    public const string OperationalException = "operational-exception";

    private static readonly HashSet<string> Supported = new(StringComparer.Ordinal)
    {
        CustomerComplaint,
        OperationalIncident,
        InternalServiceRequest,
        AccessRequest,
        ComplianceCase,
        TechnicalEscalation,
        OperationalException
    };

    public static bool IsValid(string? value) =>
        value is not null && Supported.Contains(value);
}

public static class CasePriorities
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";

    public static bool IsValid(string? value) =>
        value is Low or Medium or High or Critical;
}

public static class CaseStatuses
{
    public const string New = "new";
    public const string Assigned = "assigned";
    public const string InProgress = "in-progress";
    public const string Resolved = "resolved";
    public const string Closed = "closed";
}

public static class CaseSlaStates
{
    public const string NotApplicable = "not-applicable";
    public const string Active = "active";
    public const string Met = "met";
    public const string Breached = "breached";
}

public static class CaseTriggers
{
    public const string Assign = "assign";
    public const string StartProgress = "start-progress";
    public const string Resolve = "resolve";
    public const string Close = "close";
}

public sealed class Case : AggregateRoot<Guid>
{
    private Case()
    {
    }

    private Case(
        Guid id,
        Guid createdByUserId,
        string title,
        string description,
        string caseType,
        string priority,
        DateTimeOffset createdUtc,
        DateTimeOffset? slaTargetUtc)
        : base(id)
    {
        CreatedByUserId = createdByUserId;
        Title = title;
        Description = description;
        CaseType = caseType;
        Priority = priority;
        Status = CaseStatuses.New;
        CreatedUtc = createdUtc;
        UpdatedUtc = createdUtc;
        SlaTargetUtc = slaTargetUtc;
    }

    public Guid CreatedByUserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string CaseType { get; private set; } = string.Empty;

    public string Priority { get; private set; } = string.Empty;

    public string Status { get; private set; } = CaseStatuses.New;

    public Guid? DepartmentId { get; private set; }

    public DateTimeOffset? RoutedUtc { get; private set; }

    public Guid? AssignedToUserId { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset UpdatedUtc { get; private set; }

    public DateTimeOffset? ResolvedUtc { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public DateTimeOffset? SlaTargetUtc { get; private set; }

    public DateTimeOffset? SlaBreachedUtc { get; private set; }

    public DateTimeOffset? EscalatedUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Result<Case> Create(
        Guid createdByUserId,
        string? title,
        string? description,
        string? caseType,
        string? priority,
        DateTimeOffset createdUtc,
        DateTimeOffset? slaTargetUtc = null)
    {
        var normalizedTitle = Normalize(title);
        var normalizedDescription = Normalize(description);
        var normalizedType = NormalizeCode(caseType);
        var normalizedPriority = NormalizeCode(priority);

        if (createdByUserId == Guid.Empty)
            return Result<Case>.Failure(CaseErrors.InvalidCreator);

        if (normalizedTitle.Length is < 4 or > 160)
            return Result<Case>.Failure(CaseErrors.InvalidTitle);

        if (normalizedDescription.Length is < 10 or > 4000)
            return Result<Case>.Failure(CaseErrors.InvalidDescription);

        if (!CaseTypes.IsValid(normalizedType))
            return Result<Case>.Failure(CaseErrors.InvalidCaseType);

        if (!CasePriorities.IsValid(normalizedPriority))
            return Result<Case>.Failure(CaseErrors.InvalidPriority);

        if (slaTargetUtc.HasValue && slaTargetUtc.Value <= createdUtc)
            return Result<Case>.Failure(CaseErrors.InvalidSlaTarget);

        var @case = new Case(
            Guid.NewGuid(),
            createdByUserId,
            normalizedTitle,
            normalizedDescription,
            normalizedType,
            normalizedPriority,
            createdUtc,
            slaTargetUtc);

        @case.RaiseDomainEvent(new CaseCreated(
            @case.Id,
            @case.CreatedByUserId,
            @case.CaseType,
            @case.Priority,
            @case.CreatedUtc));

        return Result<Case>.Success(@case);
    }

    public Result RouteToDepartment(
        Guid departmentId,
        Guid routedByUserId,
        DateTimeOffset routedUtc)
    {
        if (departmentId == Guid.Empty)
            return Result.Failure(CaseErrors.InvalidDepartment);

        if (routedByUserId == Guid.Empty)
            return Result.Failure(CaseErrors.InvalidActor);

        if (Status != CaseStatuses.New
            || AssignedToUserId.HasValue
            || DepartmentId.HasValue)
        {
            return Result.Failure(CaseErrors.InvalidRoutingState);
        }

        DepartmentId = departmentId;
        RoutedUtc = routedUtc;
        UpdatedUtc = routedUtc;

        RaiseDomainEvent(new CaseRouted(
            Id,
            departmentId,
            routedByUserId,
            routedUtc));

        return Result.Success();
    }

    public Result Assign(
        Guid assigneeUserId,
        Guid assignedByUserId,
        DateTimeOffset assignedUtc)
    {
        if (assigneeUserId == Guid.Empty)
            return Result.Failure(CaseErrors.InvalidAssignee);

        if (assignedByUserId == Guid.Empty)
            return Result.Failure(CaseErrors.InvalidActor);

        if (!CaseWorkflow.Definition.TryResolve(
                Status,
                CaseTriggers.Assign,
                out var transition))
        {
            return Result.Failure(CaseErrors.InvalidTransition);
        }

        var previousStatus = Status;
        AssignedToUserId = assigneeUserId;
        Status = transition.ToState;
        UpdatedUtc = assignedUtc;

        RaiseDomainEvent(new CaseAssigned(
            Id,
            assigneeUserId,
            assignedByUserId,
            assignedUtc));
        RaiseDomainEvent(new CaseStatusChanged(
            Id,
            previousStatus,
            Status,
            assignedByUserId,
            assignedUtc));

        return Result.Success();
    }

    public Result Reassign(
        Guid assigneeUserId,
        Guid reassignedByUserId,
        DateTimeOffset reassignedUtc)
    {
        if (assigneeUserId == Guid.Empty)
            return Result.Failure(CaseErrors.InvalidAssignee);

        if (reassignedByUserId == Guid.Empty)
            return Result.Failure(CaseErrors.InvalidActor);

        if (Status is not (CaseStatuses.Assigned or CaseStatuses.InProgress)
            || !AssignedToUserId.HasValue)
        {
            return Result.Failure(CaseErrors.InvalidReassignmentState);
        }

        if (AssignedToUserId.Value == assigneeUserId)
            return Result.Failure(CaseErrors.SameAssignee);

        var previousAssigneeUserId = AssignedToUserId.Value;
        AssignedToUserId = assigneeUserId;
        UpdatedUtc = reassignedUtc;

        RaiseDomainEvent(new CaseReassigned(
            Id,
            DepartmentId,
            previousAssigneeUserId,
            assigneeUserId,
            Status,
            reassignedByUserId,
            reassignedUtc));

        return Result.Success();
    }

    public Result TransferToDepartment(
        Guid departmentId,
        Guid transferredByUserId,
        DateTimeOffset transferredUtc)
    {
        if (departmentId == Guid.Empty)
            return Result.Failure(CaseErrors.InvalidDepartment);

        if (transferredByUserId == Guid.Empty)
            return Result.Failure(CaseErrors.InvalidActor);

        if (!DepartmentId.HasValue)
            return Result.Failure(CaseErrors.TransferRequiresRouting);

        if (DepartmentId.Value == departmentId)
            return Result.Failure(CaseErrors.SameDepartment);

        if (Status is not (CaseStatuses.New or CaseStatuses.Assigned or CaseStatuses.InProgress))
            return Result.Failure(CaseErrors.InvalidTransferState);

        var previousDepartmentId = DepartmentId.Value;
        var previousAssigneeUserId = AssignedToUserId;
        var previousStatus = Status;

        DepartmentId = departmentId;
        AssignedToUserId = null;
        Status = CaseStatuses.New;
        RoutedUtc = transferredUtc;
        UpdatedUtc = transferredUtc;

        RaiseDomainEvent(new CaseTransferred(
            Id,
            previousDepartmentId,
            departmentId,
            previousAssigneeUserId,
            previousStatus,
            transferredByUserId,
            transferredUtc));

        if (previousStatus != Status)
        {
            RaiseDomainEvent(new CaseStatusChanged(
                Id,
                previousStatus,
                Status,
                transferredByUserId,
                transferredUtc));
        }

        return Result.Success();
    }

    public Result StartProgress(Guid actorUserId, DateTimeOffset changedUtc) =>
        ApplyTransition(CaseTriggers.StartProgress, actorUserId, changedUtc);

    public Result Resolve(Guid actorUserId, DateTimeOffset changedUtc)
    {
        var result = ApplyTransition(CaseTriggers.Resolve, actorUserId, changedUtc);
        if (result.IsSuccess)
            ResolvedUtc = changedUtc;

        return result;
    }

    public Result Close(Guid actorUserId, DateTimeOffset changedUtc)
    {
        var result = ApplyTransition(CaseTriggers.Close, actorUserId, changedUtc);
        if (result.IsSuccess)
            ClosedUtc = changedUtc;

        return result;
    }

    public bool EvaluateSla(DateTimeOffset evaluatedUtc)
    {
        if (!SlaTargetUtc.HasValue || SlaBreachedUtc.HasValue)
            return false;

        if (ResolvedUtc.HasValue && ResolvedUtc.Value <= SlaTargetUtc.Value)
            return false;

        var effectiveTime = ResolvedUtc ?? evaluatedUtc;
        if (effectiveTime <= SlaTargetUtc.Value)
            return false;

        SlaBreachedUtc = SlaTargetUtc.Value;
        EscalatedUtc = evaluatedUtc;
        UpdatedUtc = evaluatedUtc;

        RaiseDomainEvent(new CaseSlaBreached(
            Id,
            Priority,
            SlaTargetUtc.Value,
            evaluatedUtc));

        return true;
    }

    public string GetSlaState(DateTimeOffset evaluatedUtc)
    {
        if (!SlaTargetUtc.HasValue)
            return CaseSlaStates.NotApplicable;

        if (ResolvedUtc.HasValue)
        {
            return ResolvedUtc.Value <= SlaTargetUtc.Value
                ? CaseSlaStates.Met
                : CaseSlaStates.Breached;
        }

        if (SlaBreachedUtc.HasValue || evaluatedUtc > SlaTargetUtc.Value)
            return CaseSlaStates.Breached;

        return CaseSlaStates.Active;
    }

    private Result ApplyTransition(
        string trigger,
        Guid actorUserId,
        DateTimeOffset changedUtc)
    {
        if (actorUserId == Guid.Empty)
            return Result.Failure(CaseErrors.InvalidActor);

        if (!CaseWorkflow.Definition.TryResolve(Status, trigger, out var transition))
            return Result.Failure(CaseErrors.InvalidTransition);

        var previousStatus = Status;
        Status = transition.ToState;
        UpdatedUtc = changedUtc;

        RaiseDomainEvent(new CaseStatusChanged(
            Id,
            previousStatus,
            Status,
            actorUserId,
            changedUtc));

        return Result.Success();
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static string NormalizeCode(string? value) =>
        Normalize(value).ToLowerInvariant();
}

public sealed record CaseCreated(
    Guid CaseId,
    Guid CreatedByUserId,
    string CaseType,
    string Priority,
    DateTimeOffset CreatedUtc) : IDomainEvent;

public sealed record CaseRouted(
    Guid CaseId,
    Guid DepartmentId,
    Guid RoutedByUserId,
    DateTimeOffset RoutedUtc) : IDomainEvent;

public sealed record CaseAssigned(
    Guid CaseId,
    Guid AssigneeUserId,
    Guid AssignedByUserId,
    DateTimeOffset AssignedUtc) : IDomainEvent;

public sealed record CaseReassigned(
    Guid CaseId,
    Guid? DepartmentId,
    Guid PreviousAssigneeUserId,
    Guid AssigneeUserId,
    string Status,
    Guid ReassignedByUserId,
    DateTimeOffset ReassignedUtc) : IDomainEvent;

public sealed record CaseTransferred(
    Guid CaseId,
    Guid FromDepartmentId,
    Guid ToDepartmentId,
    Guid? PreviousAssigneeUserId,
    string PreviousStatus,
    Guid TransferredByUserId,
    DateTimeOffset TransferredUtc) : IDomainEvent;

public sealed record CaseStatusChanged(
    Guid CaseId,
    string FromStatus,
    string ToStatus,
    Guid ActorUserId,
    DateTimeOffset ChangedUtc) : IDomainEvent;

public sealed record CaseSlaBreached(
    Guid CaseId,
    string Priority,
    DateTimeOffset SlaTargetUtc,
    DateTimeOffset EvaluatedUtc) : IDomainEvent;

public static class CaseErrors
{
    public static readonly Error InvalidCreator = Error.Unauthorized(
        "Madar.InvalidCreator",
        "تعذر تحديد منشئ الحالة.");

    public static readonly Error InvalidTitle = Error.Validation(
        "Madar.InvalidTitle",
        "عنوان الحالة يجب أن يكون بين 4 و160 حرفًا.");

    public static readonly Error InvalidDescription = Error.Validation(
        "Madar.InvalidDescription",
        "وصف الحالة يجب أن يكون بين 10 و4000 حرف.");

    public static readonly Error InvalidCaseType = Error.Validation(
        "Madar.InvalidCaseType",
        "نوع الحالة غير مدعوم في الإصدار الحالي.");

    public static readonly Error InvalidPriority = Error.Validation(
        "Madar.InvalidPriority",
        "أولوية الحالة غير صالحة.");

    public static readonly Error InvalidSlaTarget = Error.Validation(
        "Madar.InvalidSlaTarget",
        "موعد SLA يجب أن يكون بعد وقت إنشاء الحالة.");

    public static readonly Error InvalidDepartment = Error.Validation(
        "Madar.InvalidDepartment",
        "القسم التشغيلي غير صالح.");

    public static readonly Error InvalidRoutingState = Error.Conflict(
        "Madar.InvalidRoutingState",
        "يمكن توجيه الحالة فقط عندما تكون جديدة وغير مسندة وغير موجهة مسبقًا.");

    public static readonly Error TransferRequiresRouting = Error.Conflict(
        "Madar.Transfer.RequiresRouting",
        "يجب أن تكون الحالة موجهة إلى قسم قبل نقلها إلى قسم آخر.");

    public static readonly Error SameDepartment = Error.Conflict(
        "Madar.Transfer.SameDepartment",
        "الحالة موجودة بالفعل في القسم المحدد.");

    public static readonly Error InvalidTransferState = Error.Conflict(
        "Madar.Transfer.InvalidState",
        "لا يمكن نقل الحالة بعد حلها أو إغلاقها.");

    public static readonly Error InvalidAssignee = Error.Validation(
        "Madar.InvalidAssignee",
        "الموظف المسند إليه غير صالح.");

    public static readonly Error InvalidReassignmentState = Error.Conflict(
        "Madar.Reassignment.InvalidState",
        "يمكن إعادة إسناد الحالة فقط عندما تكون مسندة أو قيد المعالجة.");

    public static readonly Error SameAssignee = Error.Conflict(
        "Madar.Reassignment.SameAssignee",
        "الحالة مسندة بالفعل إلى الموظف المحدد.");

    public static readonly Error InvalidActor = Error.Forbidden(
        "Madar.InvalidActor",
        "تعذر تحديد منفذ العملية.");

    public static readonly Error InvalidTransition = Error.Conflict(
        "Madar.InvalidTransition",
        "لا يمكن تنفيذ هذا الانتقال من الحالة الحالية.");
}
