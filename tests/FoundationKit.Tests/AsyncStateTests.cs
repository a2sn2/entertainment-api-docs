using System.Net;
using FoundationKit.Blazor.Api;
using FoundationKit.Blazor.State;

namespace FoundationKit.Tests;

public sealed class AsyncStateTests
{
    [Fact]
    public async Task Successful_execution_sets_value_and_clears_loading()
    {
        var state = new AsyncState<string>();

        var result = await state.ExecuteAsync(
            _ => Task.FromResult(ApiResult<string>.Success("ready", HttpStatusCode.OK)));

        Assert.True(result.IsSuccess);
        Assert.Equal("ready", state.Value);
        Assert.False(state.IsLoading);
        Assert.False(state.HasError);
    }

    [Fact]
    public async Task Failed_execution_records_error_and_preserves_previous_value()
    {
        var state = new AsyncState<string>();
        await state.ExecuteAsync(
            _ => Task.FromResult(ApiResult<string>.Success("cached", HttpStatusCode.OK)));

        var error = new ApiError("Test.Failed", "failed", HttpStatusCode.BadRequest);
        await state.ExecuteAsync(
            _ => Task.FromResult(ApiResult<string>.Failure(error)));

        Assert.Equal("cached", state.Value);
        Assert.Equal(error, state.Error);
        Assert.False(state.IsLoading);
    }

    [Fact]
    public async Task Caller_cancellation_is_not_swallowed()
    {
        var state = new AsyncState<string>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            state.ExecuteAsync(
                token => Task.FromCanceled<ApiResult<string>>(token),
                cancellation.Token));
    }
}
