using Microsoft.JSInterop;

namespace EntertainmentDocs.Admin.Infrastructure.Authentication;

public interface IAccessTokenStore
{
    ValueTask<string?> GetAsync();
    ValueTask SetAsync(string accessToken);
    ValueTask ClearAsync();
}

public sealed class BrowserAccessTokenStore(IJSRuntime jsRuntime) : IAccessTokenStore
{
    private const string StorageKey = "entertainmentdocs.admin.access-token";

    public ValueTask<string?> GetAsync() =>
        jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);

    public ValueTask SetAsync(string accessToken) =>
        jsRuntime.InvokeVoidAsync("sessionStorage.setItem", StorageKey, accessToken);

    public ValueTask ClearAsync() =>
        jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
}
