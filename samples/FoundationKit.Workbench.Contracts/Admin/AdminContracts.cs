using System.ComponentModel.DataAnnotations;

namespace FoundationKit.Workbench.Contracts.Admin;

public sealed class AdminReviewRequest
{
    [Required]
    [StringLength(20)]
    public string? Decision { get; set; }

    [Required]
    [StringLength(120)]
    public string? ReviewedBy { get; set; }

    [StringLength(1200)]
    public string? Notes { get; set; }
}

public sealed record AdminQueueItemResponse(
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
    DateTimeOffset UpdatedUtc);

public sealed record AdminReviewResponse(
    Guid ReviewId,
    Guid RequestId,
    string Status,
    string Decision,
    string ReviewedBy,
    string Notes,
    DateTimeOffset ReviewedUtc);
