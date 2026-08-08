using FoundationKit.Application.Results;

namespace Madar.Domain.Organization;

public sealed class Department
{
    private Department()
    {
    }

    private Department(
        Guid id,
        string code,
        string name,
        bool isActive,
        DateTimeOffset createdUtc)
    {
        Id = id;
        Code = code;
        Name = name;
        IsActive = isActive;
        CreatedUtc = createdUtc;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Result<Department> Create(
        string? code,
        string? name,
        DateTimeOffset createdUtc)
    {
        var normalizedCode = NormalizeCode(code);
        var normalizedName = name?.Trim() ?? string.Empty;

        if (normalizedCode.Length is < 2 or > 60
            || normalizedCode.Any(character =>
                !(char.IsLetterOrDigit(character) || character is '-' or '_')))
        {
            return Result<Department>.Failure(DepartmentErrors.InvalidCode);
        }

        if (normalizedName.Length is < 2 or > 120)
            return Result<Department>.Failure(DepartmentErrors.InvalidName);

        return Result<Department>.Success(
            new Department(
                Guid.NewGuid(),
                normalizedCode,
                normalizedName,
                true,
                createdUtc));
    }

    private static string NormalizeCode(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;
}

public sealed class DepartmentMembership
{
    private DepartmentMembership()
    {
    }

    private DepartmentMembership(
        Guid departmentId,
        Guid userId,
        DateTimeOffset joinedUtc)
    {
        DepartmentId = departmentId;
        UserId = userId;
        JoinedUtc = joinedUtc;
    }

    public Guid DepartmentId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset JoinedUtc { get; private set; }

    public static Result<DepartmentMembership> Create(
        Guid departmentId,
        Guid userId,
        DateTimeOffset joinedUtc)
    {
        if (departmentId == Guid.Empty)
            return Result<DepartmentMembership>.Failure(DepartmentErrors.InvalidDepartment);

        if (userId == Guid.Empty)
            return Result<DepartmentMembership>.Failure(DepartmentErrors.InvalidUser);

        return Result<DepartmentMembership>.Success(
            new DepartmentMembership(departmentId, userId, joinedUtc));
    }
}

public static class DepartmentErrors
{
    public static readonly Error InvalidCode = Error.Validation(
        "Madar.Department.InvalidCode",
        "رمز القسم يجب أن يكون بين حرفين و60 حرفًا ويستخدم أحرفًا أو أرقامًا أو - أو _ فقط.");

    public static readonly Error InvalidName = Error.Validation(
        "Madar.Department.InvalidName",
        "اسم القسم يجب أن يكون بين حرفين و120 حرفًا.");

    public static readonly Error InvalidDepartment = Error.Validation(
        "Madar.Department.InvalidDepartment",
        "القسم المحدد غير صالح.");

    public static readonly Error InvalidUser = Error.Validation(
        "Madar.Department.InvalidUser",
        "المستخدم المحدد غير صالح.");
}
