using FoundationKit.Blazor.Api;

namespace FoundationKit.Blazor.State;

public sealed class AsyncState<T>
{
    public T? Value { get; private set; }
    public ApiError? Error { get; private set; }
    public bool IsLoading { get; private set; }
    public bool HasValue => Value is not null;
    public bool HasError => Error is not null;

    public async Task<ApiResult<T>> ExecuteAsync(
        Func<CancellationToken, Task<ApiResult<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        IsLoading = true;
        Error = null;

        try
        {
            var result = await operation(cancellationToken);
            if (result.IsSuccess)
                Value = result.Value;
            else
                Error = result.ErrorDetails;
            return result;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Reset()
    {
        Value = default;
        Error = null;
        IsLoading = false;
    }
}
