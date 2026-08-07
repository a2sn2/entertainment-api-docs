using System.Globalization;

namespace FoundationKit.Localization;

public enum TextDirection
{
    LeftToRight,
    RightToLeft
}

public sealed record CultureDefinition
{
    public const int MaximumNameLength = 64;

    public CultureDefinition(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > MaximumNameLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(name),
                $"Culture name cannot exceed {MaximumNameLength} characters.");
        }

        var culture = CultureInfo.GetCultureInfo(trimmed);
        if (string.IsNullOrWhiteSpace(culture.Name))
        {
            throw new ArgumentException(
                "Invariant culture cannot be used as a FoundationKit culture definition.",
                nameof(name));
        }

        Name = culture.Name;
        ParentName = string.IsNullOrWhiteSpace(culture.Parent.Name)
            ? null
            : culture.Parent.Name;
        Direction = culture.TextInfo.IsRightToLeft
            ? TextDirection.RightToLeft
            : TextDirection.LeftToRight;
    }

    public string Name { get; }

    public string? ParentName { get; }

    public TextDirection Direction { get; }

    public override string ToString() => Name;
}

public sealed record TimeZoneId
{
    public const int MaximumLength = 128;

    public TimeZoneId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();

        if (normalized.Length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Time-zone ID cannot exceed {MaximumLength} characters.");
        }

        if (normalized.Any(char.IsControl))
        {
            throw new ArgumentException(
                "Time-zone ID cannot contain control characters.",
                nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public sealed record LocalizationContext
{
    public LocalizationContext(CultureDefinition culture, TimeZoneId timeZone)
    {
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
        TimeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
    }

    public CultureDefinition Culture { get; }

    public TimeZoneId TimeZone { get; }
}
