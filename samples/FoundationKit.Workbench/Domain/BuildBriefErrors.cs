using FoundationKit.Application.Results;

namespace FoundationKit.Workbench.Domain;

public static class BuildBriefErrors
{
    public static readonly Error InvalidProjectName = Error.Validation(
        "BuildBrief.InvalidProjectName",
        "Project name must contain between 2 and 160 characters.");

    public static readonly Error InvalidProjectType = Error.Validation(
        "BuildBrief.InvalidProjectType",
        "Project type must contain between 2 and 80 characters.");

    public static readonly Error InvalidAudience = Error.Validation(
        "BuildBrief.InvalidAudience",
        "Audience must contain between 2 and 300 characters.");

    public static readonly Error InvalidGoal = Error.Validation(
        "BuildBrief.InvalidGoal",
        "Goal must contain between 10 and 1000 characters.");

    public static readonly Error InvalidPriorities = Error.Validation(
        "BuildBrief.InvalidPriorities",
        "Priorities cannot exceed 800 characters.");

    public static readonly Error InvalidNotes = Error.Validation(
        "BuildBrief.InvalidNotes",
        "Notes cannot exceed 2000 characters.");
}
