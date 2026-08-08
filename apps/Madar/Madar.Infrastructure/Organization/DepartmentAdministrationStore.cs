using Madar.Application.Organization;
using Madar.Contracts.Organization;
using Madar.Domain.Cases;
using Madar.Domain.Organization;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure.Organization;

public sealed class DepartmentAdministrationStore(MadarDbContext dbContext)
    : IDepartmentAdministrationStore
{
    public async Task<IReadOnlyList<DepartmentAdminDto>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Departments
            .AsNoTracking()
            .OrderBy(item => item.Code)
            .ThenBy(item => item.Id)
            .Select(item => new DepartmentAdminDto(
                item.Id,
                item.Code,
                item.Name,
                item.IsActive,
                item.CreatedUtc,
                item.UpdatedUtc))
            .ToListAsync(cancellationToken);

    public Task<Department?> FindAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.Departments
            .SingleOrDefaultAsync(
                item => item.Id == departmentId,
                cancellationToken);

    public Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken = default) =>
        dbContext.Departments
            .AsNoTracking()
            .AnyAsync(item => item.Code == code, cancellationToken);

    public async Task AddAsync(
        Department department,
        CancellationToken cancellationToken = default) =>
        await dbContext.Departments.AddAsync(department, cancellationToken);

    public async Task<IReadOnlyList<DepartmentMemberDto>> ListMembersAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        await (
                from membership in dbContext.DepartmentMemberships.AsNoTracking()
                join user in dbContext.Users.AsNoTracking()
                    on membership.UserId equals user.Id
                where membership.DepartmentId == departmentId
                orderby user.DisplayName, user.Id
                select new DepartmentMemberDto(
                    user.Id,
                    user.DisplayName,
                    user.Email ?? string.Empty,
                    membership.JoinedUtc))
            .ToListAsync(cancellationToken);

    public Task<DepartmentMemberDto?> GetMemberAsync(
        Guid departmentId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        (
            from membership in dbContext.DepartmentMemberships.AsNoTracking()
            join user in dbContext.Users.AsNoTracking()
                on membership.UserId equals user.Id
            where membership.DepartmentId == departmentId
                && membership.UserId == userId
            select new DepartmentMemberDto(
                user.Id,
                user.DisplayName,
                user.Email ?? string.Empty,
                membership.JoinedUtc))
        .SingleOrDefaultAsync(cancellationToken);

    public Task<DepartmentMembership?> FindMembershipAsync(
        Guid departmentId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.DepartmentMemberships
            .SingleOrDefaultAsync(
                item => item.DepartmentId == departmentId
                    && item.UserId == userId,
                cancellationToken);

    public async Task AddMembershipAsync(
        DepartmentMembership membership,
        CancellationToken cancellationToken = default) =>
        await dbContext.DepartmentMemberships.AddAsync(
            membership,
            cancellationToken);

    public void RemoveMembership(DepartmentMembership membership) =>
        dbContext.DepartmentMemberships.Remove(membership);

    public Task<bool> HasOpenCasesAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.Cases
            .AsNoTracking()
            .AnyAsync(
                item => item.DepartmentId == departmentId
                    && item.Status != CaseStatuses.Closed,
                cancellationToken);

    public Task<bool> HasOpenAssignedCasesAsync(
        Guid departmentId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.Cases
            .AsNoTracking()
            .AnyAsync(
                item => item.DepartmentId == departmentId
                    && item.AssignedToUserId == userId
                    && item.Status != CaseStatuses.Closed,
                cancellationToken);
}
