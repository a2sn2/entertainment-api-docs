using Athar.Client.Services;
using Athar.Contracts;
using FoundationKit.Blazor.Mvvm;

namespace Athar.Client.ViewModels;

public sealed class AccountViewModel(
    AtharApiClient api,
    AtharAuthenticationStateProvider authenticationState)
    : ViewModelBase
{
    public LoginRequest Login { get; } = new();
    public RegisterRequest Register { get; } = new();
    public TwoFactorLoginRequest TwoFactor { get; } = new();
    public EmailAddressRequest RecoveryEmail { get; } = new();
    public ConfirmEmailRequest Confirmation { get; } = new();
    public ResetPasswordRequest PasswordReset { get; } = new();
    public ChangePasswordRequest PasswordChange { get; } = new();
    public MfaSetupRequest MfaSetupRequest { get; } = new();
    public MfaCodeRequest MfaCode { get; } = new();
    public MfaDisableRequest MfaDisable { get; } = new();
    public MfaRecoveryCodesRequest MfaRecoveryCodes { get; } = new();

    public CurrentUserResponse? CurrentUser { get; private set; }
    public MfaStatusResponse? MfaStatus { get; private set; }
    public MfaSetupResponse? MfaSetup { get; private set; }
    public IReadOnlyList<string> RecoveryCodes { get; private set; } = [];
    public string? SuccessMessage { get; private set; }
    public bool RequiresTwoFactor { get; private set; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var current = await api.GetCurrentUserAsync();
        if (current.IsSuccess && current.Value is not null && current.Value.IsAuthenticated)
        {
            CurrentUser = current.Value;
            await LoadMfaStatusCoreAsync();
        }
        NotifyStateChanged();
    });

    public Task LoginAsync() => RunAsync(async () =>
    {
        SuccessMessage = null;
        RequiresTwoFactor = false;
        var result = await api.LoginAsync(Login);
        if (result.IsFailure || result.Value is null)
        {
            if (result.ErrorDetails?.Code == "TwoFactorRequired")
            {
                RequiresTwoFactor = true;
                SuccessMessage = "أدخل رمز تطبيق المصادقة أو أحد رموز الاسترداد لإكمال الدخول.";
                NotifyStateChanged();
                return;
            }

            SetError(result.Error ?? "تعذر تسجيل الدخول.");
            return;
        }

        await CompleteAuthenticationAsync(result.Value);
    });

    public Task CompleteTwoFactorLoginAsync() => RunAsync(async () =>
    {
        SuccessMessage = null;
        var result = await api.TwoFactorLoginAsync(TwoFactor);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر التحقق من رمز المصادقة الثنائية.");
            return;
        }

        RequiresTwoFactor = false;
        await CompleteAuthenticationAsync(result.Value);
    });

    public Task RegisterAsync() => RunAsync(async () =>
    {
        SuccessMessage = null;
        var result = await api.RegisterAsync(Register);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر إنشاء الحساب.");
            return;
        }

        CurrentUser = result.Value;
        if (!result.Value.IsAuthenticated)
        {
            SuccessMessage = "تم إنشاء الحساب. تحقق من بريدك الإلكتروني ثم سجل الدخول.";
            NotifyStateChanged();
            return;
        }

        await CompleteAuthenticationAsync(result.Value);
    });

    public Task RequestConfirmationAsync() => RunMessageAsync(
        () => api.RequestEmailConfirmationAsync(RecoveryEmail),
        "تم استلام الطلب. إذا كان الحساب مؤهلًا فستصل تعليمات التأكيد إلى البريد المسجل.");

    public Task ConfirmEmailAsync() => RunMessageAsync(
        () => api.ConfirmEmailAsync(Confirmation),
        "تم تأكيد البريد الإلكتروني.");

    public Task ForgotPasswordAsync() => RunMessageAsync(
        () => api.ForgotPasswordAsync(RecoveryEmail),
        "تم استلام الطلب. إذا كان الحساب مؤهلًا فستصل تعليمات الاستعادة إلى البريد المسجل.");

    public Task ResetPasswordAsync() => RunMessageAsync(
        () => api.ResetPasswordAsync(PasswordReset),
        "تم تحديث كلمة المرور. سجل الدخول من جديد.");

    public Task ChangePasswordAsync() => RunAsync(async () =>
    {
        var result = await api.ChangePasswordAsync(PasswordChange);
        if (result.IsFailure)
        {
            SetError(result.Error ?? "تعذر تغيير كلمة المرور.");
            return;
        }

        PasswordChange.CurrentPassword = string.Empty;
        PasswordChange.NewPassword = string.Empty;
        SuccessMessage = result.Value?.Message ?? "تم تغيير كلمة المرور.";
        NotifyStateChanged();
    });

    public Task SetupMfaAsync() => RunAsync(async () =>
    {
        RecoveryCodes = [];
        var result = await api.SetupMfaAsync(MfaSetupRequest);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر إعداد المصادقة الثنائية.");
            return;
        }

        MfaSetup = result.Value;
        SuccessMessage = "أضف المفتاح إلى تطبيق المصادقة ثم أدخل الرمز المكون من ستة أرقام.";
        NotifyStateChanged();
    });

    public Task EnableMfaAsync() => RunAsync(async () =>
    {
        var result = await api.EnableMfaAsync(MfaCode);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر تفعيل المصادقة الثنائية.");
            return;
        }

        RecoveryCodes = result.Value.RecoveryCodes;
        CurrentUser = null;
        MfaStatus = null;
        authenticationState.Refresh();
        SuccessMessage = "تم تفعيل المصادقة الثنائية. احفظ رموز الاسترداد في مكان آمن ثم سجل الدخول من جديد.";
        NotifyStateChanged();
    });

    public Task DisableMfaAsync() => RunAsync(async () =>
    {
        var result = await api.DisableMfaAsync(MfaDisable);
        if (result.IsFailure)
        {
            SetError(result.Error ?? "تعذر تعطيل المصادقة الثنائية.");
            return;
        }

        MfaDisable.CurrentPassword = string.Empty;
        MfaDisable.Code = string.Empty;
        CurrentUser = null;
        MfaStatus = null;
        MfaSetup = null;
        RecoveryCodes = [];
        authenticationState.Refresh();
        SuccessMessage = result.Value?.Message;
        NotifyStateChanged();
    });

    public Task RegenerateRecoveryCodesAsync() => RunAsync(async () =>
    {
        var result = await api.RegenerateRecoveryCodesAsync(MfaRecoveryCodes);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر إنشاء رموز استرداد جديدة.");
            return;
        }

        MfaRecoveryCodes.CurrentPassword = string.Empty;
        MfaRecoveryCodes.Code = string.Empty;
        RecoveryCodes = result.Value.RecoveryCodes;
        SuccessMessage = "تم إبطال رموز الاسترداد السابقة وإنشاء مجموعة جديدة.";
        await LoadMfaStatusCoreAsync();
    });

    private async Task RunMessageAsync<T>(
        Func<Task<FoundationKit.Blazor.Api.ApiResult<T>>> operation,
        string successMessage)
    {
        await RunAsync(async () =>
        {
            var result = await operation();
            if (result.IsFailure)
            {
                SetError(result.Error ?? "تعذر إكمال العملية.");
                return;
            }

            SuccessMessage = successMessage;
            NotifyStateChanged();
        });
    }

    private async Task CompleteAuthenticationAsync(CurrentUserResponse user)
    {
        CurrentUser = user;
        authenticationState.Refresh();
        SuccessMessage = $"مرحبًا {user.DisplayName}";
        await LoadMfaStatusCoreAsync();
        NotifyStateChanged();
    }

    private async Task LoadMfaStatusCoreAsync()
    {
        var result = await api.GetMfaStatusAsync();
        if (result.IsSuccess)
            MfaStatus = result.Value;
    }
}

public sealed class InitiativesViewModel(AtharApiClient api)
    : ListViewModel<InitiativeSummaryDto>
{
    public CreateInitiativeRequest Draft { get; private set; } = new();
    public InitiativeDetailsDto? LastCreated { get; private set; }
    public InitiativeDetailsDto? Selected { get; private set; }
    public string? Search { get; set; }
    public string? Status { get; set; }

    public Task LoadAsync() => RunAsync(LoadCoreAsync);

    public Task CreateAsync() => RunAsync(async () =>
    {
        var result = await api.CreateInitiativeAsync(Draft);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر إنشاء المبادرة.");
            return;
        }
        LastCreated = result.Value;
        Selected = result.Value;
        Draft = new CreateInitiativeRequest();
        await LoadCoreAsync();
    });

    public Task SelectAsync(Guid id) => RunAsync(async () =>
    {
        var result = await api.GetInitiativeAsync(id);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر تحميل تفاصيل المبادرة.");
            return;
        }
        Selected = result.Value;
        NotifyStateChanged();
    });

    private async Task LoadCoreAsync()
    {
        var result = await api.GetMyInitiativesAsync(search: Search, status: Status);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر تحميل المبادرات.");
            return;
        }
        Items = result.Value.Items;
        NotifyStateChanged();
    }
}

public sealed class AdminViewModel(AtharApiClient api)
    : ListViewModel<InitiativeSummaryDto>
{
    public AdminDashboardResponse? Dashboard { get; private set; }
    public InitiativeDetailsDto? Selected { get; private set; }
    public string Status { get; set; } = InitiativeWorkflow.Submitted;
    public string? Search { get; set; }

    public Task LoadAsync() => RunAsync(LoadCoreAsync);

    public Task SelectAsync(Guid id) => RunAsync(async () =>
    {
        var result = await api.GetInitiativeAsync(id);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر تحميل تفاصيل المبادرة.");
            return;
        }
        Selected = result.Value;
        NotifyStateChanged();
    });

    public Task ReviewAsync(Guid initiativeId, string decision, string? notes) => RunAsync(async () =>
    {
        var result = await api.ReviewInitiativeAsync(
            initiativeId,
            new ReviewInitiativeRequest { Decision = decision, Notes = notes });
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر حفظ قرار المراجعة.");
            return;
        }
        Selected = result.Value;
        await LoadCoreAsync();
    });

    private async Task LoadCoreAsync()
    {
        var dashboardTask = api.GetAdminDashboardAsync();
        var queueTask = api.GetAdminInitiativesAsync(search: Search, status: Status);
        await Task.WhenAll(dashboardTask, queueTask);
        var dashboard = await dashboardTask;
        var queue = await queueTask;

        if (dashboard.IsFailure || dashboard.Value is null)
        {
            SetError(dashboard.Error ?? "تعذر تحميل لوحة الإدارة.");
            return;
        }
        if (queue.IsFailure || queue.Value is null)
        {
            SetError(queue.Error ?? "تعذر تحميل قائمة المبادرات.");
            return;
        }
        Dashboard = dashboard.Value;
        Items = queue.Value.Items;
        NotifyStateChanged();
    }
}
