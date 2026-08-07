using System.Globalization;

namespace FoundationKit.Localization;

public enum CultureResolutionSource
{
    Exact,
    Parent,
    Default,
    InvalidRequested
}

public sealed record CultureResolution(
    CultureDefinition Culture,
    CultureResolutionSource Source)
{
    public override string ToString() => $"{Culture.Name}:{Source}";
}

public sealed class SupportedCultureSet
{
    public const int MaximumSupportedCultures = 32;

    private readonly Dictionary<string, CultureDefinition> _cultures;

    public SupportedCultureSet(
        IEnumerable<string> supportedCultureNames,
        string defaultCultureName)
    {
        ArgumentNullException.ThrowIfNull(supportedCultureNames);

        var cultures = new Dictionary<string, CultureDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in supportedCultureNames)
        {
            var culture = new CultureDefinition(name);
            if (!cultures.TryAdd(culture.Name, culture))
            {
                throw new ArgumentException(
                    $"Duplicate supported culture '{culture.Name}' is not allowed.",
                    nameof(supportedCultureNames));
            }

            if (cultures.Count > MaximumSupportedCultures)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(supportedCultureNames),
                    $"Supported culture count cannot exceed {MaximumSupportedCultures}.");
            }
        }

        if (cultures.Count == 0)
        {
            throw new ArgumentException(
                "At least one supported culture is required.",
                nameof(supportedCultureNames));
        }

        var defaultCulture = new CultureDefinition(defaultCultureName);
        if (!cultures.TryGetValue(defaultCulture.Name, out var canonicalDefault))
        {
            throw new ArgumentException(
                "Default culture must be included in the supported culture set.",
                nameof(defaultCultureName));
        }

        _cultures = cultures;
        DefaultCulture = canonicalDefault;
        Cultures = Array.AsReadOnly(cultures.Values
            .OrderBy(culture => culture.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray());
    }

    public CultureDefinition DefaultCulture { get; }

    public IReadOnlyList<CultureDefinition> Cultures { get; }

    public CultureResolution Resolve(string? requestedCultureName)
    {
        if (string.IsNullOrWhiteSpace(requestedCultureName))
        {
            return new CultureResolution(DefaultCulture, CultureResolutionSource.Default);
        }

        CultureInfo requested;
        try
        {
            requested = CultureInfo.GetCultureInfo(requestedCultureName.Trim());
        }
        catch (CultureNotFoundException)
        {
            return new CultureResolution(DefaultCulture, CultureResolutionSource.InvalidRequested);
        }

        if (string.IsNullOrWhiteSpace(requested.Name))
        {
            return new CultureResolution(DefaultCulture, CultureResolutionSource.InvalidRequested);
        }

        if (_cultures.TryGetValue(requested.Name, out var exact))
        {
            return new CultureResolution(exact, CultureResolutionSource.Exact);
        }

        var parent = requested.Parent;
        while (!string.IsNullOrWhiteSpace(parent.Name))
        {
            if (_cultures.TryGetValue(parent.Name, out var supportedParent))
            {
                return new CultureResolution(supportedParent, CultureResolutionSource.Parent);
            }

            parent = parent.Parent;
        }

        return new CultureResolution(DefaultCulture, CultureResolutionSource.Default);
    }
}
