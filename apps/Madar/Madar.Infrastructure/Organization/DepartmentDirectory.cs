using Madar.Application.Organization;
using Madar.Contracts.Organization;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure.Organization;

public sealed class DepartmentDirectory(MadarDbContext dbContext) : IDepartmentDirectory
{
    public async Task<DepartmentDto?> GetAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Departments
            .AsNoTracking()
            .Where(item => item.Id == departmentId)
            .Select(item => new DepartmentDto(
                item.Id,
                item.Code,
                item.Name,
                item.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<DepartmentDto>> ListActiveAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Departments
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Select(item => new DepartmentDto(
                item.Id,
                item.Code,
                item.Name,
                item.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DepartmentDto>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await (
                from membership in dbContext.DepartmentMemberships.AsNoTracking()
                join department in dbContext.Departments.AsNoTracking()
                    on membership.DepartmentId equals department.Id
                where membership.UserId == userId && department.IsActive
                orderby department.Name, department.Id
                select new DepartmentDto(
                    department.Id,
                    department.Code,
                    department.Name,
                    department.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<bool> IsMemberAsync(
        Guid departmentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (departmentId == Guid.Empty || userId == Guid.Empty)
            return false;

        return await (
                from membership in dbContext.DepartmentMemberships.AsNoTracking()
                join department in dbContext.Departments.AsNoTracking()
                    on membership.DepartmentId equals department.Id
                where membership.DepartmentId == departmentId
                    && membership.UserId == userId
                    && department.IsActive
                select membership.UserId)
            .AnyAsync(cancellationToken);
    }
}
