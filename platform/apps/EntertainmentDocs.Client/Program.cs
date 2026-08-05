using EntertainmentDocs.Client;
using EntertainmentDocs.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuredApiBase = builder.Configuration["ApiBaseUrl"];
var applicationBase = new Uri(builder.HostEnvironment.BaseAddress);
var apiBase = string.IsNullOrWhiteSpace(configuredApiBase)
    ? new Uri(applicationBase, "../")
    : Uri.TryCreate(configuredApiBase, UriKind.Absolute, out var absoluteApiBase)
        ? absoluteApiBase
        : new Uri(applicationBase, configuredApiBase);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiBase });
builder.Services.AddScoped<DocumentationApiClient>();
await builder.Build().RunAsync();
