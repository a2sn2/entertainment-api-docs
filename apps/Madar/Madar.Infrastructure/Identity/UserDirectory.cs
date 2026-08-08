using Madar.Application.Security;
using Madar.Contracts.Security;
using Microsoft.AspNetCore.Identity;

namespace Madar.Infrastructure.Identity;

public sealed class UserDirectory(UserManager<MadarUser> userManager) : IUserDirectory
{
    public async Task<bool> IsAssignableOperatorAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return false;

        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId.ToString("D"));
        return user is not null
            && await userManager.IsInRoleAsync(user, MadarRoles.Operator);
    }
}
