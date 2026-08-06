using Athar.Client;
using Athar.Client.Services;
using Athar.Client.ViewModels;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AtharApiClient>();
builder.Services.AddScoped<AtharAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(services =>
    services.GetRequiredService<AtharAuthenticationStateProvider>());
builder.Services.AddScoped<AccountViewModel>();
builder.Services.AddScoped<InitiativesViewModel>();
builder.Services.AddScoped<AdminViewModel>();

await builder.Build().RunAsync();
