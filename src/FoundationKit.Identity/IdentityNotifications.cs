namespace FoundationKit.Identity;

public enum AccountSecurityNotification
{
    PasswordChanged,
    PasswordReset,
    MfaEnabled,
    MfaDisabled,
    RecoveryCodesRegenerated
}

public interface IAccountNotificationSender
{
    Task<bool> SendEmailConfirmationAsync(
        string destinationEmail,
        string confirmationToken,
        CancellationToken cancellationToken = default);

    Task<bool> SendPasswordResetAsync(
        string destinationEmail,
        string resetToken,
        CancellationToken cancellationToken = default);

    Task<bool> SendSecurityNotificationAsync(
        string destinationEmail,
        AccountSecurityNotification notification,
        CancellationToken cancellationToken = default);
}
