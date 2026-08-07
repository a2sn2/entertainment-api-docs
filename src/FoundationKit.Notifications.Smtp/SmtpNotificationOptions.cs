namespace FoundationKit.Notifications.Smtp;

public sealed class SmtpNotificationOptions
{
    public const int DefaultPort = 587;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = DefaultPort;

    public bool EnableSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;
}

public static class SmtpNotificationOptionsValidator
{
    public static void Validate(SmtpNotificationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "SMTP port must be between 1 and 65535.");
        }

        EnsureNoControlCharacters(options.Host, nameof(options.Host));
        EnsureNoControlCharacters(options.Username, nameof(options.Username));
        EnsureNoControlCharacters(options.FromAddress, nameof(options.FromAddress));
    }

    private static void EnsureNoControlCharacters(string? value, string parameterName)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (value.Any(char.IsControl))
        {
            throw new ArgumentException(
                "SMTP option values cannot contain control characters.",
                parameterName);
        }
    }
}
