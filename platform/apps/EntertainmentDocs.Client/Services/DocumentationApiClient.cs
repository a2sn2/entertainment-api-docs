using System.Net.Http.Json;

namespace EntertainmentDocs.Client.Services;

public sealed class DocumentationApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<DocumentSummary>> ListAsync(CancellationToken ct = default) =>
        await httpClient.GetFromJsonAsync<DocumentSummary[]>("api/v1/documents", ct) ?? [];
}

public sealed record DocumentSummary(Guid Id, string Reference, string Slug, string Title, string Status, DateTimeOffset UpdatedAt);
