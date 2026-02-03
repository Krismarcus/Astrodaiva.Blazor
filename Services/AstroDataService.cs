using System.Net.Http.Json;
using Astrodaiva.Data;
using Astrodaiva.Data.Models;

namespace Astrodaiva.Blazor.Services;

public class AstroDataService
{
    private readonly HttpClient _http;
    public AstroDataService(HttpClient http) => _http = http;

    public async Task<AppDB?> LoadAsync(string path = "data/astrodb.json")
    {
        try { return await _http.GetFromJsonAsync<AppDB>(path); }
        catch { return null; }
    }
}
