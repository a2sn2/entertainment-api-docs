using System.Net;
using System.Text;
using FoundationKit.Blazor.Api;

namespace FoundationKit.Tests;

public sealed class ApiClientTests
{
    [Fact]
    public async Task Invalid_json_success_response_becomes_typed_failure()
    {
        using var httpClient = new HttpClient(new StubHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{not-json", Encoding.UTF8, "application/json")
            }));

        var client = new TestClient(httpClient);
        var result = await client.GetAsync<Payload>();

        Assert.True(result.IsFailure);
        Assert.Equal("Response.InvalidJson", result.ErrorDetails?.Code);
    }

    [Fact]
    public async Task Problem_details_error_preserves_code_and_correlation()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                "{\"title\":\"Item.NotFound\",\"detail\":\"Missing.\",\"correlationId\":\"abc\"}",
                Encoding.UTF8,
                "application/problem+json")
        };

        using var httpClient = new HttpClient(new StubHandler(response));
        var client = new TestClient(httpClient);
        var result = await client.GetAsync<Payload>();

        Assert.True(result.IsFailure);
        Assert.Equal("Item.NotFound", result.ErrorDetails?.Code);
        Assert.Equal("abc", result.ErrorDetails?.CorrelationId);
    }

    private sealed record Payload(string Name);

    private sealed class TestClient(HttpClient httpClient) : ApiClientBase(httpClient)
    {
        public Task<ApiResult<T>> GetAsync<T>() =>
            SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, "https://localhost/test"));
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
