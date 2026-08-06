using System.Text.Json;
using FoundationKit.Application.Results;
using FoundationKit.Domain.Primitives;

namespace FoundationKit.Workbench.Domain;

public sealed class BuildBrief : AggregateRoot<Guid>
{
    private BuildBrief()
    {
    }

    private BuildBrief(
        Guid id,
        string projectName,
        string projectType,
        string audience,
        string goal,
        string selectedCapabilityIdsJson,
        string priorities,
        string notes,
        DateTimeOffset createdUtc)
        : base(id)
    {
        ProjectName = projectName;
        ProjectType = projectType;
        Audience = audience;
        Goal = goal;
        SelectedCapabilityIdsJson = selectedCapabilityIdsJson;
        Priorities = priorities;
        Notes = notes;
        Status = BuildBriefStatus.Submitted;
        CreatedUtc = createdUtc;
        UpdatedUtc = createdUtc;
    }

    public string ProjectName { get; private set; } = string.Empty;
    public string ProjectType { get; private set; } = string.Empty;
    public string Audience { get; private set; } = string.Empty;
    public string Goal { get; private set; } = string.Empty;
    public string SelectedCapabilityIdsJson { get; private set; } = "[]";
    public string Priorities { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public BuildBriefStatus Status { get; private set; } = BuildBriefStatus.Submitted;
    public DateTimeOffset CreatedUtc { get; private set; }
    public DateTimeOffset UpdatedUtc { get; private set; }

    public IReadOnlyList<string> SelectedCapabilityIds =>
        JsonSerializer.Deserialize<string[]>(SelectedCapabilityIdsJson) ?? [];

    public static Result<BuildBrief> Create(
        string? projectName,
        string? projectType,
        string? audience,
        string? goal,
        IReadOnlyCollection<string>? selectedCapabilityIds,
        string? priorities,
        string? notes,
        DateTimeOffset createdUtc)
    {
        var normalizedName = Normalize(projectName);
        var normalizedType = Normalize(projectType);
        var normalizedAudience = Normalize(audience);
        var normalizedGoal = Normalize(goal);
        var normalizedPriorities = Normalize(priorities);
        var normalizedNotes = Normalize(notes);

        if (normalizedName.Length is < 2 or > 160)
            return Result<BuildBrief>.Failure(BuildBriefErrors.InvalidProjectName);

        if (normalizedType.Length is < 2 or > 80)
            return Result<BuildBrief>.Failure(BuildBriefErrors.InvalidProjectType);

        if (normalizedAudience.Length is < 2 or > 300)
            return Result<BuildBrief>.Failure(BuildBriefErrors.InvalidAudience);

        if (normalizedGoal.Length is < 10 or > 1000)
            return Result<BuildBrief>.Failure(BuildBriefErrors.InvalidGoal);

        if (normalizedPriorities.Length > 800)
            return Result<BuildBrief>.Failure(BuildBriefErrors.InvalidPriorities);

        if (normalizedNotes.Length > 2000)
            return Result<BuildBrief>.Failure(BuildBriefErrors.InvalidNotes);

        var capabilities = (selectedCapabilityIds ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Take(40)
            .ToArray();

        var brief = new BuildBrief(
            Guid.NewGuid(),
            normalizedName,
            normalizedType,
            normalizedAudience,
            normalizedGoal,
            JsonSerializer.Serialize(capabilities),
            normalizedPriorities,
            normalizedNotes,
            createdUtc);

        brief.RaiseDomainEvent(new BuildBriefCreated(brief.Id, brief.ProjectName, brief.CreatedUtc));
        return Result<BuildBrief>.Success(brief);
    }

    public Result<BuildBrief> ApplyReview(
        AdminReviewDecision decision,
        DateTimeOffset reviewedUtc)
    {
        if (Status is not BuildBriefStatus.Submitted)
            return Result<BuildBrief>.Failure(BuildBriefErrors.AlreadyReviewed);

        Status = decision is AdminReviewDecision.Approve
            ? BuildBriefStatus.Approved
            : BuildBriefStatus.Rejected;
        UpdatedUtc = reviewedUtc;

        RaiseDomainEvent(new BuildBriefReviewed(Id, Status, reviewedUtc));
        return Result<BuildBrief>.Success(this);
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
