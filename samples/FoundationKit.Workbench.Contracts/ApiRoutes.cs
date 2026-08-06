namespace FoundationKit.Workbench.Contracts;

public static class ApiRoutes
{
    public const string Runtime = "api/runtime";
    public const string Catalog = "api/catalog";
    public const string Health = "api/health";

    public static class User
    {
        public const string Requests = "api/user/requests";

        public static string Request(Guid id) => $"{Requests}/{id:D}";
    }

    public static class Admin
    {
        public const string Requests = "api/admin/requests";

        public static string Review(Guid id) => $"{Requests}/{id:D}/review";
    }
}
