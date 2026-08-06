using FoundationKit.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Infrastructure.Persistence;

public sealed class EfUnitOfWork<TDbContext>(TDbContext dbContext) : IUnitOfWork
    where TDbContext : DbContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
