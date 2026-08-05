using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EntertainmentDocs.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultLocalConnection =
        "Server=localhost;Database=EntertainmentDocs_Dev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
            ?? Environment.GetEnvironmentVariable("ENTERTAINMENTDOCS_SQLSERVER")
            ?? DefaultLocalConnection;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure())
            .Options;

        return new AppDbContext(options);
    }
}
