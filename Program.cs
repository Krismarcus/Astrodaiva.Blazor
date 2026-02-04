using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Astrodaiva.Blazor;
using Astrodaiva.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register services
builder.Services.AddSingleton<AppState>();
// Local (static) assets on the Blazor site
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromSeconds(20)
});

// API client (snapshots, default startup JSON)
builder.Services.AddScoped(sp =>
{
    var apiBase = builder.Configuration["ApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(apiBase))
        apiBase = builder.HostEnvironment.BaseAddress;

    return new AstroApiClient(new HttpClient
    {
        BaseAddress = new Uri(apiBase),
        // If API host is unreachable (common on GH Pages), do NOT stall the app.
        Timeout = TimeSpan.FromSeconds(4)
    });
});

builder.Services.AddScoped<AstroDbStore>();
builder.Services.AddScoped<Astrodaiva.Blazor.Services.AstroDataService>();
builder.Services.AddScoped<AstroDbEditService>();

await builder.Build().RunAsync();