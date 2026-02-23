using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
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

    // Ensure trailing slash (avoids URL concatenation bugs)
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

await builder.Build().RunAsync();