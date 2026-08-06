using FoundationKit.Workbench.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class WorkbenchDbContext(DbContextOptions<WorkbenchDbContext> options)
    : DbContext(options)
{
    public DbSet<BuildBrief> BuildBriefs => Set<BuildBrief>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkbenchDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
