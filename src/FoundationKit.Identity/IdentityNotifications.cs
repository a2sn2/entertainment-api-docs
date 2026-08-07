namespace FoundationKit.Identity;

public enum IdentitySecurityNotification
{
    PasswordChanged,
    PasswordReset,
    MfaEnabled,
    MfaDisabled,
    RecoveryCodesRegenerated
}

public interface IIdentityNotificationSender
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
        IdentitySecurityNotification notification,
        CancellationToken cancellationToken = default);
}
