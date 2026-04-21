using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Astrodaiva.Blazor;
using Astrodaiva.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ---------- STATE ----------
builder.Services.AddSingleton<AppState>();

// ---------- STATIC FILES (local Blazor assets) ----------
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromSeconds(20)
});

// ---------- API CLIENT ----------
builder.Services.AddScoped(sp =>
{
    var apiBase = builder.Configuration["ApiBaseUrl"];

    if (string.IsNullOrWhiteSpace(apiBase))
        throw new InvalidOperationException("ApiBaseUrl is not configured.");

    if (!apiBase.EndsWith("/"))
        apiBase += "/";

    var http = new HttpClient
    {
        BaseAddress = new Uri(apiBase),
        Timeout = TimeSpan.FromSeconds(8)
    };

    return new AstroApiClient(http);
});

// ---------- DATA SERVICES ----------
builder.Services.AddScoped<AstroDbStore>();
builder.Services.AddScoped<AstroDataService>();
builder.Services.AddScoped<AstroDbEditService>();
builder.Services.AddScoped<AdminAccessService>();

var host = builder.Build();

try
{
    var js = host.Services.GetRequiredService<IJSRuntime>();
    var cultureName = await js.InvokeAsync<string>("blazorCulture.get");
    if (string.IsNullOrWhiteSpace(cultureName))
        cultureName = "lt-LT";

    var culture = new CultureInfo(cultureName);
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
}
catch
{
    var fallback = new CultureInfo("lt-LT");
    CultureInfo.DefaultThreadCurrentCulture = fallback;
    CultureInfo.DefaultThreadCurrentUICulture = fallback;
}

await host.RunAsync();
