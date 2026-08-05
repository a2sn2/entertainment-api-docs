using FoundationKit.Domain.Primitives;

namespace FoundationKit.Application.Persistence;

public interface IReadRepository<TEntity, in TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<TEntity>? specification = null, CancellationToken cancellationToken = default);
}
