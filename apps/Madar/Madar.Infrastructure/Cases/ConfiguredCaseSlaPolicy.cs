using Madar.Application.Cases;
using Madar.Domain.Cases;
using Microsoft.Extensions.Options;

namespace Madar.Infrastructure.Cases;

public sealed class MadarSlaOptions
{
    public const string SectionName = "Madar:Sla";

    public bool Enabled { get; set; }

    public TimeSpan? Low { get; set; }

    public TimeSpan? Medium { get; set; }

    public TimeSpan? High { get; set; }

    public TimeSpan? Critical { get; set; }

    public IEnumerable<TimeSpan?> Durations =>
    [
        Low,
        Medium,
        High,
        Critical
    ];
}

public sealed class ConfiguredCaseSlaPolicy(IOptions<MadarSlaOptions> options)
    : ICaseSlaPolicy
{
    private readonly MadarSlaOptions _options = options.Value;

    public TimeSpan? ResolveDuration(string? priority)
    {
        if (!_options.Enabled)
            return null;

        var normalized = priority?.Trim().ToLowerInvariant();
        return normalized switch
        {
            CasePriorities.Low => _options.Low,
            CasePriorities.Medium => _options.Medium,
            CasePriorities.High => _options.High,
            CasePriorities.Critical => _options.Critical,
            _ => null
        };
    }
}
