using Microsoft.AspNetCore.Identity;

namespace Athar.Infrastructure;

public sealed class AtharUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }
}

public sealed class AuditEntry
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Details { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }
}

public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public bool Enabled { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = "مسؤول منصة أثر";

    public string Password { get; set; } = string.Empty;
}

public sealed class DatabaseStartupOptions
{
    public const string SectionName = "DatabaseStartup";

    public bool ApplyMigrationsOnStartup { get; set; }

    public bool SeedRolesOnStartup { get; set; }

    public int MigrationAttempts { get; set; } = 60;

    public int DelaySeconds { get; set; } = 2;
}
