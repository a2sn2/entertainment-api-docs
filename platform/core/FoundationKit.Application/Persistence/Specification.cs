using System.Linq.Expressions;

namespace FoundationKit.Application.Persistence;

public abstract class Specification<TEntity> : ISpecification<TEntity>
{
    private readonly List<Expression<Func<TEntity, object>>> _includes = [];

    protected Specification(Expression<Func<TEntity, bool>>? criteria = null) => Criteria = criteria;

    public Expression<Func<TEntity, bool>>? Criteria { get; }
    public IReadOnlyList<Expression<Func<TEntity, object>>> Includes => _includes;
    public Expression<Func<TEntity, object>>? OrderBy { get; private set; }
    public Expression<Func<TEntity, object>>? OrderByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool AsNoTracking { get; private set; }

    protected void AddInclude(Expression<Func<TEntity, object>> includeExpression) => _includes.Add(includeExpression);

    protected void ApplyOrderBy(Expression<Func<TEntity, object>> expression)
    {
        OrderBy = expression;
        OrderByDescending = null;
    }

    protected void ApplyOrderByDescending(Expression<Func<TEntity, object>> expression)
    {
        OrderByDescending = expression;
        OrderBy = null;
    }

    protected void ApplyPaging(int skip, int take)
    {
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        if (take <= 0) throw new ArgumentOutOfRangeException(nameof(take));
        Skip = skip;
        Take = take;
    }

    protected void UseNoTracking() => AsNoTracking = true;
}
