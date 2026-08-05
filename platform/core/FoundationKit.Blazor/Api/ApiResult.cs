using System.Net;

namespace FoundationKit.Blazor.Api;

public class ApiResult
{
    protected ApiResult(bool isSuccess, ApiError? error, HttpStatusCode? statusCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public ApiError? Error { get; }
    public HttpStatusCode? StatusCode { get; }

    public static ApiResult Success(HttpStatusCode? statusCode = null) => new(true, null, statusCode);
    public static ApiResult Failure(ApiError error) => new(false, error, error.StatusCode);
}

public sealed class ApiResult<T> : ApiResult
{
    private ApiResult(T value, HttpStatusCode? statusCode) : base(true, null, statusCode) => Value = value;
    private ApiResult(ApiError error) : base(false, error, error.StatusCode) { }

    public T? Value { get; }

    public static ApiResult<T> Success(T value, HttpStatusCode? statusCode = null) => new(value, statusCode);
    public new static ApiResult<T> Failure(ApiError error) => new(error);
}
