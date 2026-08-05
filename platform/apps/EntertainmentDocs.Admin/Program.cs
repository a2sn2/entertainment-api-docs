using EntertainmentDocs.Admin;
using EntertainmentDocs.Admin.Features.Authentication;
using EntertainmentDocs.Admin.Features.Documents;
using EntertainmentDocs.Admin.Features.Users;
using EntertainmentDocs.Admin.Infrastructure.Api;
using EntertainmentDocs.Admin.Infrastructure.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

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
builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<IAccessTokenStore, BrowserAccessTokenStore>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(services =>
    services.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddScoped<AuthenticatedRequestFactory>();

builder.Services.AddScoped<AuthenticationApiClient>();
builder.Services.AddScoped<UsersApiClient>();
builder.Services.AddScoped<DocumentsApiClient>();

await builder.Build().RunAsync();
