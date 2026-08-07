using FoundationKit.Identity;
using Xunit;

namespace FoundationKit.Tests;

public sealed class IdentityCapabilityTests
{
    [Fact]
    public void Default_account_security_policy_is_valid()
    {
        AccountSecurityOptionsValidator.Validate(new AccountSecurityOptions());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(129)]
    public void Account_security_policy_rejects_unsupported_password_lengths(int length)
    {
        var options = new AccountSecurityOptions
        {
            PasswordRequiredLength = length
        };

        Assert.Throws<InvalidOperationException>(() =>
            AccountSecurityOptionsValidator.Validate(options));
    }

    [Theory]
    [InlineData(IdentitySensitiveOperation.ChangePassword, IdentityStepUpFactor.Password)]
    [InlineData(IdentitySensitiveOperation.SetupMultiFactor, IdentityStepUpFactor.Password)]
    [InlineData(
        IdentitySensitiveOperation.DisableMultiFactor,
        IdentityStepUpFactor.Password | IdentityStepUpFactor.MultiFactor)]
    [InlineData(
        IdentitySensitiveOperation.RegenerateRecoveryCodes,
        IdentityStepUpFactor.Password | IdentityStepUpFactor.MultiFactor)]
    public void Sensitive_operations_have_explicit_step_up_requirements(
        IdentitySensitiveOperation operation,
        IdentityStepUpFactor expected)
    {
        Assert.Equal(expected, IdentityStepUpPolicy.RequiredFactors(operation));
    }

    [Fact]
    public void Full_reauthentication_requires_both_password_and_multi_factor()
    {
        Assert.False(IdentityStepUpPolicy.IsSatisfied(
            IdentitySensitiveOperation.DisableMultiFactor,
            passwordVerified: false,
            multiFactorVerified: true));
        Assert.False(IdentityStepUpPolicy.IsSatisfied(
            IdentitySensitiveOperation.DisableMultiFactor,
            passwordVerified: true,
            multiFactorVerified: false));
        Assert.True(IdentityStepUpPolicy.IsSatisfied(
            IdentitySensitiveOperation.DisableMultiFactor,
            passwordVerified: true,
            multiFactorVerified: true));
    }

    [Fact]
    public async Task Notification_port_keeps_delivery_behind_a_provider_contract()
    {
        IAccountNotificationSender sender = new RecordingNotificationSender();

        Assert.True(await sender.SendSecurityNotificationAsync(
            "user@example.test",
            AccountSecurityNotification.MfaDisabled));
    }

    private sealed class RecordingNotificationSender : IAccountNotificationSender
    {
        public Task<bool> SendEmailConfirmationAsync(
            string destinationEmail,
            string confirmationToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> SendPasswordResetAsync(
            string destinationEmail,
            string resetToken,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> SendSecurityNotificationAsync(
            string destinationEmail,
            AccountSecurityNotification notification,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
