namespace FoundationKit.Settings;

public static class SettingKey
{
    public const int MaximumLength = 160;

    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Setting key cannot exceed {MaximumLength} characters.");
        }

        if (!char.IsLetterOrDigit(normalized[0])
            || normalized.Any(character =>
                !(char.IsLetterOrDigit(character)
                  || character is '.' or ':' or '-' or '_')))
        {
            throw new ArgumentException(
                "Setting key must start with a letter or digit and contain only letters, digits, '.', ':', '-', or '_'.",
                nameof(value));
        }

        return normalized;
    }
}

public static class SettingScopeKind
{
    public const int MaximumLength = 64;
    public const string Global = "global";

    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Setting scope kind cannot exceed {MaximumLength} characters.");
        }

        if (!char.IsLetterOrDigit(normalized[0])
            || normalized.Any(character =>
                !(char.IsLetterOrDigit(character)
                  || character is '-' or '_')))
        {
            throw new ArgumentException(
                "Setting scope kind must start with a letter or digit and contain only letters, digits, '-', or '_'.",
                nameof(value));
        }

        return normalized;
    }
}

public sealed record SettingScope
{
    public const int MaximumIdentifierLength = 200;

    public static SettingScope Global { get; } = new(SettingScopeKind.Global, null);

    public SettingScope(string kind, string? identifier)
    {
        Kind = SettingScopeKind.Normalize(kind);

        if (string.Equals(Kind, SettingScopeKind.Global, StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException(
                    "The global setting scope cannot have an identifier.",
                    nameof(identifier));
            }

            Identifier = null;
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        var normalizedIdentifier = identifier.Trim();
        if (normalizedIdentifier.Length > MaximumIdentifierLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(identifier),
                $"Setting scope identifier cannot exceed {MaximumIdentifierLength} characters.");
        }

        if (normalizedIdentifier.IndexOf('\0', StringComparison.Ordinal) >= 0)
        {
            throw new ArgumentException(
                "Setting scope identifier cannot contain a null character.",
                nameof(identifier));
        }

        Identifier = normalizedIdentifier;
    }

    public string Kind { get; }

    public string? Identifier { get; }

    public override string ToString() => Identifier is null ? Kind : $"{Kind}:{Identifier}";
}

public sealed record SettingEntry
{
    public const int MaximumValueLength = 16_384;

    public SettingEntry(SettingScope scope, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length > MaximumValueLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Setting value cannot exceed {MaximumValueLength} characters.");
        }

        if (value.IndexOf('\0', StringComparison.Ordinal) >= 0)
        {
            throw new ArgumentException(
                "Setting value cannot contain a null character.",
                nameof(value));
        }

        Scope = scope;
        Key = SettingKey.Normalize(key);
        Value = value;
    }

    public SettingScope Scope { get; }

    public string Key { get; }

    public string Value { get; }

    public override string ToString() => $"{Scope}/{Key}";
}

public sealed class SettingResolutionContext
{
    public const int MaximumScopes = 16;

    public static SettingResolutionContext Global { get; } = new([]);

    public SettingResolutionContext(IEnumerable<SettingScope> scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        var ordered = new List<SettingScope>();
        foreach (var scope in scopes)
        {
            ArgumentNullException.ThrowIfNull(scope);
            if (scope == SettingScope.Global || ordered.Contains(scope))
            {
                continue;
            }

            ordered.Add(scope);
            if (ordered.Count > MaximumScopes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scopes),
                    $"A setting resolution context cannot exceed {MaximumScopes} non-global scopes.");
            }
        }

        ordered.Add(SettingScope.Global);
        Scopes = ordered.AsReadOnly();
    }

    public IReadOnlyList<SettingScope> Scopes { get; }
}

public sealed record ResolvedSetting(
    string Key,
    string Value,
    SettingScope Scope)
{
    public override string ToString() => $"{Scope}/{Key}";
}
