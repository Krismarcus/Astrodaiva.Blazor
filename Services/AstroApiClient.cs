using System.Net;
using System.Net.Http.Json;

namespace Astrodaiva.Blazor.Services;

/// <summary>
/// Thin wrapper around the ASP.NET API (Astrodaiva.Api) endpoints.
/// </summary>
public class AstroApiClient
{
    private readonly HttpClient _http;

    public AstroApiClient(HttpClient http) => _http = http;

    /// <summary>Returns raw JSON of the default snapshot, or null if none exists / API not reachable.</summary>
    public async Task<string?> TryGetDefaultSnapshotJsonAsync()
    {
        try
        {
            var resp = await _http.GetAsync("api/import/default");
            if (resp.StatusCode == HttpStatusCode.NotFound) return null;
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<SnapshotItemDto>> ListSnapshotsAsync(int take = 80)
    {
        take = Math.Clamp(take, 1, 200);
        return await _http.GetFromJsonAsync<List<SnapshotItemDto>>($"api/import/snapshots?take={take}")
               ?? new List<SnapshotItemDto>();
    }

    public async Task<string> GetSnapshotJsonAsync(long id)
        => await _http.GetStringAsync($"api/import/snapshots/{id}");

    public async Task DeleteSnapshotAsync(long id)
    {
        var resp = await _http.DeleteAsync($"api/import/snapshots/{id}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<string> CreateSnapshotAsync(string appDbJson, string? label, bool setDefault)
    {
        // API expects a JSON body (SaveSnapshotRequest), not raw AppDB JSON.
        var payload = new SaveSnapshotRequest(label, setDefault, appDbJson);

        // Note: keep this relative (BaseAddress points to the API host)
        var resp = await _http.PostAsJsonAsync("api/import/full-sync", payload);
        var body = await resp.Content.ReadAsStringAsync();
        resp.EnsureSuccessStatusCode();
        return body;
    }

    public async Task SetDefaultSnapshotAsync(long id)
    {
        var resp = await _http.PostAsync($"api/import/snapshots/{id}/set-default", content: null);
        resp.EnsureSuccessStatusCode();
    }

    public record SaveSnapshotRequest(string? Label, bool SetDefault, string Json);

    public record SnapshotItemDto(long Id, DateTime CreatedUtc, string? Label, bool IsDefault, int SizeBytes);
}
