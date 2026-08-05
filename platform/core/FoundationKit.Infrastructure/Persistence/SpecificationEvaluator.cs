using FoundationKit.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Infrastructure.Persistence;

public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> Apply<TEntity>(
        IQueryable<TEntity> query,
        ISpecification<TEntity>? specification)
        where TEntity : class
    {
        if (specification is null)
            return query;

        if (specification.AsNoTracking)
            query = query.AsNoTracking();

        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
            query = query.OrderBy(specification.OrderBy);
        else if (specification.OrderByDescending is not null)
            query = query.OrderByDescending(specification.OrderByDescending);

        if (specification.Skip is int skip)
            query = query.Skip(skip);
        if (specification.Take is int take)
            query = query.Take(take);

        return query;
    }
}
