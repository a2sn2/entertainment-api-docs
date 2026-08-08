using Microsoft.AspNetCore.Identity;

namespace Madar.Infrastructure.Identity;

public sealed class MadarUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }
}
