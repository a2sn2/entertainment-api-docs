using System.Globalization;
using System.Text;
using FoundationKit.Workbench.Application;
using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Infrastructure;

public sealed class ContactLinkBuilder : IContactLinkBuilder
{
    public string Build(BuildBrief brief)
    {
        ArgumentNullException.ThrowIfNull(brief);

        var summary = new StringBuilder()
            .AppendLine("FoundationKit Workbench")
            .AppendLine(FormattableString.Invariant($"Project: {brief.ProjectName}"))
            .AppendLine(FormattableString.Invariant($"Type: {brief.ProjectType}"))
            .AppendLine(FormattableString.Invariant($"Goal: {brief.Goal}"))
            .AppendLine(FormattableString.Invariant($"Audience: {brief.Audience}"))
            .AppendLine(FormattableString.Invariant($"Contact: {brief.ContactName}"))
            .AppendLine(FormattableString.Invariant($"Email: {brief.ContactEmail}"))
            .ToString();

        return "https://github.com/a2sn2?tab=repositories&q="
            + Uri.EscapeDataString(summary.Trim());
    }
}
