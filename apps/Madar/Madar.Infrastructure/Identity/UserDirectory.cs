using Madar.Application.Security;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure.Identity;

public sealed class UserDirectory(MadarDbContext dbContext) : IUserDirectory
{
    public Task<bool> ExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        userId != Guid.Empty
            ? dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken)
            : Task.FromResult(false);
}
