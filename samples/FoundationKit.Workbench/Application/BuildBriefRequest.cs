namespace FoundationKit.Workbench.Application;

public sealed record BuildBriefRequest(
    string? ProjectName,
    string? ProjectType,
    string? Audience,
    string? Goal,
    IReadOnlyCollection<string>? SelectedCapabilityIds,
    string? Priorities,
    string? Notes);
