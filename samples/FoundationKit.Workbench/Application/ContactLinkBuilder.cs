using System.Text;
using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Application;

public static class ContactLinkBuilder
{
    private const string NewIssueUrl =
        "https://github.com/a2sn2/foundationkit-dotnet/issues/new";

    public static string Build(BuildBrief brief)
    {
        var title = $"Build inquiry: {brief.ProjectName}";
        var body = new StringBuilder()
            .AppendLine("## FoundationKit build inquiry")
            .AppendLine()
            .AppendLine($"- **Project:** {brief.ProjectName}")
            .AppendLine($"- **Type:** {brief.ProjectType}")
            .AppendLine($"- **Audience:** {brief.Audience}")
            .AppendLine($"- **Goal:** {brief.Goal}")
            .AppendLine($"- **Priorities:** {ValueOrDash(brief.Priorities)}")
            .AppendLine($"- **Capabilities:** {string.Join(", ", brief.SelectedCapabilityIds)}")
            .AppendLine()
            .AppendLine("### Notes")
            .AppendLine(ValueOrDash(brief.Notes))
            .AppendLine()
            .AppendLine("> This GitHub issue is public. Confidential details must be shared through an agreed private channel.")
            .ToString();

        return $"{NewIssueUrl}?title={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(body)}";
    }

    private static string ValueOrDash(string value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;
}
