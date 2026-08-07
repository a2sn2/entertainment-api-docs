using System.Text;
using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Application;

public static class ContactLinkBuilder
{
    private const string NewIssueUrl =
        "https://github.com/a2sn2/foundationkit-dotnet/issues/new";

    public static string Build(BuildBrief brief)
    {
        ArgumentNullException.ThrowIfNull(brief);

        var title = FormattableString.Invariant($"Build inquiry: {brief.ProjectName}");
        var body = new StringBuilder()
            .AppendLine("## FoundationKit build inquiry")
            .AppendLine()
            .AppendLine(FormattableString.Invariant($"- **Project:** {brief.ProjectName}"))
            .AppendLine(FormattableString.Invariant($"- **Type:** {brief.ProjectType}"))
            .AppendLine(FormattableString.Invariant($"- **Audience:** {brief.Audience}"))
            .AppendLine(FormattableString.Invariant($"- **Goal:** {brief.Goal}"))
            .AppendLine(FormattableString.Invariant($"- **Priorities:** {ValueOrDash(brief.Priorities)}"))
            .AppendLine(FormattableString.Invariant($"- **Capabilities:** {string.Join(", ", brief.SelectedCapabilityIds)}"))
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
