using System.Net.Http.Headers;
using EntertainmentDocs.Admin.Infrastructure.Authentication;

namespace EntertainmentDocs.Admin.Infrastructure.Api;

public sealed class AuthenticatedRequestFactory(IAccessTokenStore tokenStore)
{
    public async Task<HttpRequestMessage> CreateAsync(
        HttpMethod method,
        string requestUri,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = content
        };

        var accessToken = await tokenStore.GetAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return request;
    }
}
