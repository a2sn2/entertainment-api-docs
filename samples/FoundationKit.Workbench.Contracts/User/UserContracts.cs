using System.ComponentModel.DataAnnotations;

namespace FoundationKit.Workbench.Contracts.User;

public sealed class CreateUserRequest
{
    [Required]
    [StringLength(160)]
    public string? ProjectName { get; set; }

    [Required]
    [StringLength(80)]
    public string? ProjectType { get; set; }

    [Required]
    [StringLength(300)]
    public string? Audience { get; set; }

    [Required]
    [StringLength(1000)]
    public string? Goal { get; set; }

    public IReadOnlyCollection<string> SelectedCapabilityIds { get; set; } = [];

    [StringLength(800)]
    public string? Priorities { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public sealed record UserRequestResponse(
    Guid Id,
    string ProjectName,
    string ProjectType,
    string Audience,
    string Goal,
    IReadOnlyList<string> SelectedCapabilityIds,
    string Priorities,
    string Notes,
    string Status,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    string ContactUrl);
