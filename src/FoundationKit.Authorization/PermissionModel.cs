namespace FoundationKit.Authorization;

public sealed record PermissionDefinition
{
    public PermissionDefinition(
        string id,
        string displayName,
        string? description = null)
    {
        Id = PermissionId.Normalize(id);
        DisplayName = RequireText(displayName, nameof(displayName), 120);
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : RequireText(description, nameof(description), 500);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string? Description { get; }

    private static string RequireText(string value, string parameterName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}

public static class PermissionId
{
    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 160)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Permission ID cannot exceed 160 characters.");
        }

        if (!char.IsLetterOrDigit(normalized[0])
            || normalized.Any(character =>
                !(char.IsLetterOrDigit(character)
                  || character is '.' or ':' or '-' or '_')))
        {
            throw new ArgumentException(
                "Permission ID must start with a letter or digit and contain only letters, digits, '.', ':', '-', or '_'.",
                nameof(value));
        }

        return normalized;
    }
}

public sealed record RolePermissionGrant
{
    public RolePermissionGrant(
        string role,
        IEnumerable<string> permissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentNullException.ThrowIfNull(permissions);

        Role = role.Trim();
        if (Role.Length > 120)
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                "Role cannot exceed 120 characters.");
        }

        Permissions = permissions
            .Select(PermissionId.Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (Permissions.Count == 0)
        {
            throw new ArgumentException(
                "A role permission grant must contain at least one permission.",
                nameof(permissions));
        }
    }

    public string Role { get; }

    public IReadOnlyList<string> Permissions { get; }
}
