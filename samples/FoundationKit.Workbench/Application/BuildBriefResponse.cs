using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Application;

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
    string ContactUrl)
{
    public static BuildBriefResponse From(BuildBrief brief) => new(
        brief.Id,
        brief.ProjectName,
        brief.ProjectType,
        brief.Audience,
        brief.Goal,
        brief.SelectedCapabilityIds,
        brief.Priorities,
        brief.Notes,
        brief.CreatedUtc,
        ContactLinkBuilder.Build(brief));
}
