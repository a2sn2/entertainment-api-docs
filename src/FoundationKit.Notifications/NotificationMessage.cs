namespace FoundationKit.Notifications;

public sealed class NotificationMessage
{
    public const int MaxDestinationLength = 512;
    public const int MaxTitleLength = 256;
    public const int MaxBodyLength = 16_384;
    public const int MaxPurposeLength = 96;

    private NotificationMessage(
        string destination,
        string title,
        string body,
        string purpose)
    {
        Destination = destination;
        Title = title;
        Body = body;
        Purpose = purpose;
    }

    public string Destination { get; }

    public string Title { get; }

    public string Body { get; }

    public string Purpose { get; }

    public static NotificationMessage Create(
        string destination,
        string title,
        string body,
        string purpose)
    {
        return new NotificationMessage(
            RequiredText(destination, nameof(destination), MaxDestinationLength, allowLineBreaks: false),
            RequiredText(title, nameof(title), MaxTitleLength, allowLineBreaks: false),
            RequiredText(body, nameof(body), MaxBodyLength, allowLineBreaks: true),
            RequiredCode(purpose, nameof(purpose), MaxPurposeLength));
    }

    public override string ToString() =>
        $"{nameof(NotificationMessage)}({Purpose})";

    private static string RequiredText(
        string value,
        string parameterName,
        int maxLength,
        bool allowLineBreaks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Value cannot exceed {maxLength} characters.");
        }

        foreach (var character in normalized)
        {
            if (!char.IsControl(character)
                || (allowLineBreaks && character is '\r' or '\n' or '\t'))
            {
                continue;
            }

            throw new ArgumentException(
                "Notification text cannot contain unsupported control characters.",
                parameterName);
        }

        return normalized;
    }

    private static string RequiredCode(
        string value,
        string parameterName,
        int maxLength)
    {
        var normalized = RequiredText(
            value,
            parameterName,
            maxLength,
            allowLineBreaks: false);

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character) || character is '.' or '_' or '-' or ':')
            {
                continue;
            }

            throw new ArgumentException(
                "Notification purpose codes may contain only letters, digits, '.', '_', '-', or ':'.",
                parameterName);
        }

        return normalized;
    }
}
