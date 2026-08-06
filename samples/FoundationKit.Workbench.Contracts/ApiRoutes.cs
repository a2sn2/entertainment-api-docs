namespace FoundationKit.Workbench.Contracts;

public static class ApiRoutes
{
    public const string Runtime = "api/runtime";
    public const string Catalog = "api/catalog";
    public const string Health = "api/health";
    public const string BuildBriefs = "api/build-briefs";

    public static string BuildBrief(Guid id) => $"{BuildBriefs}/{id:D}";
}
