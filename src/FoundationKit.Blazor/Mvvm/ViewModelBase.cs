namespace FoundationKit.Blazor.Mvvm;

public abstract class ViewModelBase : IDisposable
{
    public event Action? StateChanged;

    public bool IsBusy { get; private set; }

    public string? ErrorMessage { get; private set; }

    protected async Task RunAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        IsBusy = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyStateChanged();
        }
    }

    protected void SetError(string? message)
    {
        ErrorMessage = message;
        NotifyStateChanged();
    }

    protected void NotifyStateChanged() => StateChanged?.Invoke();

    public virtual void Dispose() => StateChanged = null;
}

public abstract class ListViewModel<TItem> : ViewModelBase
{
    public IReadOnlyList<TItem> Items { get; protected set; } = [];
}
