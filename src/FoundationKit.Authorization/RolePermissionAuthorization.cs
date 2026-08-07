namespace FoundationKit.Authorization;

public sealed class RolePermissionMap
{
    private readonly Dictionary<string, IReadOnlyList<string>> _rolesByPermission;

    public RolePermissionMap(IEnumerable<RolePermissionGrant> grants)
    {
        ArgumentNullException.ThrowIfNull(grants);

        var rolesByPermission = new Dictionary<string, HashSet<string>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var grant in grants)
        {
            ArgumentNullException.ThrowIfNull(grant);

            foreach (var permission in grant.Permissions)
            {
                if (!rolesByPermission.TryGetValue(permission, out var roles))
                {
                    roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    rolesByPermission[permission] = roles;
                }

                roles.Add(grant.Role);
            }
        }

        _rolesByPermission = rolesByPermission.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)Array.AsReadOnly(
                pair.Value.Order(StringComparer.OrdinalIgnoreCase).ToArray()),
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> RolesFor(string permission)
    {
        var normalized = PermissionId.Normalize(permission);
        return _rolesByPermission.TryGetValue(normalized, out var roles)
            ? roles
            : Array.Empty<string>();
    }
}

public interface IAuthorizationEvaluator
{
    bool HasPermission(string permission);

    bool CanAccessOwnedResource(
        Guid ownerUserId,
        string privilegedPermission);
}

public sealed class RolePermissionAuthorizationEvaluator(
    IAuthorizationSubject subject,
    RolePermissionMap permissions) : IAuthorizationEvaluator
{
    public bool HasPermission(string permission)
    {
        var normalized = PermissionId.Normalize(permission);

        if (!subject.IsAuthenticated)
        {
            return false;
        }

        return permissions.RolesFor(normalized).Any(subject.IsInRole);
    }

    public bool CanAccessOwnedResource(
        Guid ownerUserId,
        string privilegedPermission)
    {
        var normalizedPermission = PermissionId.Normalize(privilegedPermission);

        if (!subject.IsAuthenticated || subject.UserId is null)
        {
            return false;
        }

        return subject.UserId.Value == ownerUserId
            || HasPermission(normalizedPermission);
    }
}
