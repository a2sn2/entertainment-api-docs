using FoundationKit.Domain.Primitives;

namespace FoundationKit.Application.Persistence;

public interface IRepository<TEntity, TId> : IReadRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    void Remove(TEntity entity);

    void RemoveRange(IEnumerable<TEntity> entities);
}
