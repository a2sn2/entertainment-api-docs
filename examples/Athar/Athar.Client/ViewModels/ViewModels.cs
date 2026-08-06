using Athar.Client.Services;
using Athar.Contracts;
using FoundationKit.Application.Pagination;
using FoundationKit.Blazor.Mvvm;

namespace Athar.Client.ViewModels;

public sealed class AccountViewModel(
    AtharApiClient api,
    AtharAuthenticationStateProvider authenticationState)
    : ViewModelBase
{
    public LoginRequest Login { get; } = new();

    public RegisterRequest Register { get; } = new();

    public CurrentUserResponse? CurrentUser { get; private set; }

    public Task LoginAsync() => RunAsync(async () =>
    {
        var result = await api.LoginAsync(Login);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر تسجيل الدخول.");
            return;
        }

        CurrentUser = result.Value;
        authenticationState.Refresh();
        NotifyStateChanged();
    });

    public Task RegisterAsync() => RunAsync(async () =>
    {
        var result = await api.RegisterAsync(Register);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر إنشاء الحساب.");
            return;
        }

        CurrentUser = result.Value;
        authenticationState.Refresh();
        NotifyStateChanged();
    });
}

public sealed class InitiativesViewModel(AtharApiClient api)
    : ListViewModel<InitiativeSummaryDto>
{
    public CreateInitiativeRequest Draft { get; private set; } = new();

    public InitiativeDetailsDto? LastCreated { get; private set; }

    public string? Search { get; set; }

    public string? Status { get; set; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var result = await api.GetMyInitiativesAsync(
            search: Search,
            status: Status);

        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر تحميل المبادرات.");
            return;
        }

        Items = result.Value.Items;
        NotifyStateChanged();
    });

    public Task CreateAsync() => RunAsync(async () =>
    {
        var result = await api.CreateInitiativeAsync(Draft);
        if (result.IsFailure || result.Value is null)
        {
            SetError(result.Error ?? "تعذر إنشاء المبادرة.");
            return;
        }

        LastCreated = result.Value;
        Draft = new CreateInitiativeRequest();
        NotifyStateChanged();
        await LoadAsync();
    });
}

public sealed class AdminViewModel(AtharApiClient api)
    : ListViewModel<InitiativeSummaryDto>
{
    public AdminDashboardResponse? Dashboard { get; private set; }

    public string Status { get; set; } = "submitted";

    public string? Search { get; set; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var dashboardTask = api.GetAdminDashboardAsync();
        var queueTask = api.GetAdminInitiativesAsync(
            search: Search,
            status: Status);

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
    });

    public Task ReviewAsync(
        Guid initiativeId,
        string decision,
        string? notes) => RunAsync(async () =>
    {
        var result = await api.ReviewInitiativeAsync(
            initiativeId,
            new ReviewInitiativeRequest
            {
                Decision = decision,
                Notes = notes
            });

        if (result.IsFailure)
        {
            SetError(result.Error ?? "تعذر حفظ قرار المراجعة.");
            return;
        }

        await LoadAsync();
    });
}
