using System.Net.Http.Json;

namespace FoundationKit.Blazor.Api;

public abstract class ApiClientBase(HttpClient httpClient)
{
    protected HttpClient HttpClient { get; } = httpClient;

    protected async Task<ApiResult> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? ApiResult.Success(response.StatusCode)
                : ApiResult.Failure(await ApiResponseReader.ReadErrorAsync(response, cancellationToken));
        }
        catch (HttpRequestException exception)
        {
            return ApiResult.Failure(ApiResponseReader.NetworkFailure(exception));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult.Failure(ApiResponseReader.Timeout());
        }
    }

    protected async Task<ApiResult<T>> SendAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ApiResult<T>.Failure(await ApiResponseReader.ReadErrorAsync(response, cancellationToken));

            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return value is null
                ? ApiResult<T>.Failure(new ApiError(
                    "Response.Empty",
                    "The API returned an empty response.",
                    response.StatusCode))
                : ApiResult<T>.Success(value, response.StatusCode);
        }
        catch (HttpRequestException exception)
        {
            return ApiResult<T>.Failure(ApiResponseReader.NetworkFailure(exception));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult<T>.Failure(ApiResponseReader.Timeout());
        }
    }
}
