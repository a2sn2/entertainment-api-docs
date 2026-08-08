using FoundationKit.Notifications;
using FoundationKit.Notifications.Smtp;

namespace Madar.Api;

public static class MadarNotificationServices
{
    private const string SmtpSectionName = "Madar:Notifications:Smtp";

    public static IServiceCollection AddMadarNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration
            .GetSection(SmtpSectionName)
            .Get<SmtpNotificationOptions>()
            ?? new SmtpNotificationOptions();
        SmtpNotificationOptionsValidator.Validate(configured);

        var options = new SmtpNotificationOptions
        {
            Host = configured.Host?.Trim() ?? string.Empty,
            Port = configured.Port,
            EnableSsl = configured.EnableSsl,
            Username = configured.Username?.Trim() ?? string.Empty,
            Password = configured.Password ?? string.Empty,
            FromAddress = configured.FromAddress?.Trim() ?? string.Empty
        };

        services.AddSingleton(options);
        services.AddScoped<INotificationSender>(serviceProvider =>
            new SmtpNotificationSender(
                serviceProvider.GetRequiredService<SmtpNotificationOptions>()));

        return services;
    }
}
