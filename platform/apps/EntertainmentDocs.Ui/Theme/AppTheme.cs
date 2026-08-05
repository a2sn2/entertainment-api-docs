using MudBlazor;

namespace EntertainmentDocs.Ui.Theme;

public static class AppTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1F5EFF",
            Secondary = "#00A896",
            AppbarBackground = "#0F172A",
            AppbarText = "#FFFFFF",
            Background = "#F7F9FC",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1F2937",
            TextPrimary = "#111827",
            TextSecondary = "#667085",
            Success = "#14804A",
            Warning = "#B54708",
            Error = "#B42318",
            Info = "#175CD3"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7AA2FF",
            Secondary = "#4DD8C4",
            AppbarBackground = "#0B1220",
            AppbarText = "#F8FAFC",
            Background = "#0F172A",
            Surface = "#111827",
            DrawerBackground = "#111827",
            DrawerText = "#E5E7EB",
            TextPrimary = "#F8FAFC",
            TextSecondary = "#CBD5E1",
            Success = "#32D583",
            Warning = "#FDB022",
            Error = "#F97066",
            Info = "#53B1FD"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px"
        }
    };
}
