using FoundationKit.Application.Persistence;
using FoundationKit.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Infrastructure.Persistence;

public class EfRepository<TEntity, TId, TDbContext>(TDbContext dbContext)
    : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
    where TDbContext : DbContext
{
    protected TDbContext DbContext { get; } = dbContext;

    protected DbSet<TEntity> Set => DbContext.Set<TEntity>();

    public virtual Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(entity => entity.Id.Equals(id), cancellationToken);

    public virtual Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default) =>
        SpecificationEvaluator
            .Apply(Set.AsQueryable(), specification)
            .FirstOrDefaultAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default) =>
        await SpecificationEvaluator
            .Apply(Set.AsQueryable(), specification)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public virtual Task<int> CountAsync(
        ISpecification<TEntity>? specification = null,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsQueryable();
        if (specification?.Criteria is not null)
            query = query.Where(specification.Criteria);

        return query.CountAsync(cancellationToken);
    }

    public virtual Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default) =>
        Set.AddAsync(entity, cancellationToken).AsTask();

    public virtual Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default) =>
        Set.AddRangeAsync(entities, cancellationToken);

    public virtual void Remove(TEntity entity) => Set.Remove(entity);

    public virtual void RemoveRange(IEnumerable<TEntity> entities) =>
        Set.RemoveRange(entities);
}
