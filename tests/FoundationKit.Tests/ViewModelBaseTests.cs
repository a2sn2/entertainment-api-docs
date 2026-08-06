using FoundationKit.Blazor.Mvvm;

namespace FoundationKit.Tests;

public sealed class ViewModelBaseTests
{
    [Fact]
    public async Task Run_async_notifies_busy_and_completion_states()
    {
        var viewModel = new TestViewModel();
        var notifications = 0;
        viewModel.StateChanged += () => notifications++;

        await viewModel.ExecuteAsync();

        Assert.False(viewModel.IsBusy);
        Assert.Null(viewModel.ErrorMessage);
        Assert.True(viewModel.Completed);
        Assert.True(notifications >= 2);
    }

    [Fact]
    public async Task Run_async_exposes_operation_error_without_crashing_component()
    {
        var viewModel = new TestViewModel();

        await viewModel.FailAsync();

        Assert.False(viewModel.IsBusy);
        Assert.Equal("expected", viewModel.ErrorMessage);
    }

    private sealed class TestViewModel : ViewModelBase
    {
        public bool Completed { get; private set; }

        public Task ExecuteAsync() => RunAsync(() =>
        {
            Completed = true;
            return Task.CompletedTask;
        });

        public Task FailAsync() => RunAsync(() =>
            Task.FromException(new InvalidOperationException("expected")));
    }
}
