using System.ComponentModel.DataAnnotations;

namespace FoundationKit.Workbench.Contracts;

public sealed class BuildBriefRequest
{
    [Required]
    [StringLength(120)]
    public string? ProjectName { get; set; }

    [Required]
    [StringLength(80)]
    public string? ProjectType { get; set; }

    [Required]
    [StringLength(160)]
    public string? Audience { get; set; }

    [Required]
    [StringLength(1200)]
    public string? Goal { get; set; }

    public IReadOnlyCollection<string> SelectedCapabilityIds { get; set; } = [];

    [StringLength(800)]
    public string? Priorities { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public sealed record BuildBriefResponse(
    Guid Id,
    string ProjectName,
    string ProjectType,
    string Audience,
    string Goal,
    IReadOnlyList<string> SelectedCapabilityIds,
    string Priorities,
    string Notes,
    DateTimeOffset CreatedUtc,
    string ContactUrl);
