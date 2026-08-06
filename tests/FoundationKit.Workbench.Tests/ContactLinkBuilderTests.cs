using FoundationKit.Workbench.Application;
using FoundationKit.Workbench.Domain;

namespace FoundationKit.Workbench.Tests;

public sealed class ContactLinkBuilderTests
{
    [Fact]
    public void Contact_link_targets_repository_and_contains_encoded_summary()
    {
        var result = BuildBrief.Create(
            "Support Hub",
            "Messaging",
            "Customer service",
            "Route support conversations and track escalations.",
            ["correlation-id"],
            "Fast response",
            "Public-safe summary",
            DateTimeOffset.UtcNow);

        var url = ContactLinkBuilder.Build(result.Value);

        Assert.StartsWith(
            "https://github.com/a2sn2/foundationkit-dotnet/issues/new?",
            url);
        Assert.Contains("Build%20inquiry", url);
        Assert.Contains("Support%20Hub", url);
    }
}
