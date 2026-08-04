namespace EntertainmentDocs.Infrastructure.Identity;

public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string Editor = "Editor";
    public const string Reviewer = "Reviewer";
    public const string Reader = "Reader";

    public static readonly string[] All = [Administrator, Editor, Reviewer, Reader];
}
