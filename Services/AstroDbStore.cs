using System.Text.Json;
using System.Net.Http.Json;
using Astrodaiva.Data.Models;

namespace Astrodaiva.Blazor.Services;

/// <summary>
/// Single source of truth for the AppDB JSON in the Blazor client.
///
/// Load order:
/// 1) Local wwwroot/data/astrodb.json (fast, always available on GH Pages)
/// 2) Default snapshot from API (if present) – overrides local
/// </summary>
public class AstroDbStore
{
    private readonly HttpClient _localHttp;
    private readonly AstroApiClient _api;

    private bool _apiRefreshStarted;
    private Task? _apiRefreshTask;

    public AstroDbStore(HttpClient localHttp, AstroApiClient api)
    {
        _localHttp = localHttp;
        _api = api;
    }

    public AppDB? Db { get; private set; }
    public bool IsLoaded => Db is not null;

    public event Action? Changed;

    public async Task<AppDB?> EnsureLoadedAsync()
    {
        if (Db is not null) return Db;

        // DB-first startup:
        // 1) Try API default snapshot first, but NEVER block app startup for long.
        // 2) If there is no snapshot (or API is unreachable/slow), fall back to local JSON.
        //
        // NOTE: App.razor waits for this method before showing Router, so keep it fast.

        const int apiTimeoutMs = 1200;
        var apiTask = _api.TryGetDefaultSnapshotJsonAsync();

        try
        {
            var completed = await Task.WhenAny(apiTask, Task.Delay(apiTimeoutMs));
            if (completed == apiTask)
            {
                var json = await apiTask; // may be null/empty when no snapshot exists
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var apiDb = Deserialize(json);
                    if (apiDb is not null)
                    {
                        Db = apiDb;
                        Changed?.Invoke();
                        return Db;
                    }
                }
                // No snapshot (or failed to deserialize) -> fall through to local JSON.
            }
            else
            {
                // Timed out: keep loading local JSON now, but allow API task to finish later and override.
                _ = apiTask.ContinueWith(t =>
                {
                    try
                    {
                        if (t.Status != TaskStatus.RanToCompletion) return;
                        var json = t.Result;
                        if (string.IsNullOrWhiteSpace(json)) return;
                        var apiDb = Deserialize(json);
                        if (apiDb is null) return;
                        Db = apiDb;
                        Changed?.Invoke();
                    }
                    catch
                    {
                        // ignore
                    }
                });
            }
        }
        catch
        {
            // Ignore API failures; we'll fall back to local JSON.
        }

        // Fallback: local JSON (single canonical location)
        try
        {
            Db = await _localHttp.GetFromJsonAsync<AppDB>("data/astrodb.json");
        }
        catch
        {
            Db = null;
        }

        Changed?.Invoke();
        return Db;
    }

    private void StartApiRefreshInBackground()
    {
        if (_apiRefreshStarted) return;
        _apiRefreshStarted = true;

        _apiRefreshTask = RefreshFromApiAsync();
    }

    private async Task RefreshFromApiAsync()
    {
        try
        {
            var json = await _api.TryGetDefaultSnapshotJsonAsync();
            if (string.IsNullOrWhiteSpace(json)) return;

            var apiDb = Deserialize(json);
            if (apiDb is null) return;

            Db = apiDb;
            Changed?.Invoke();
        }
        catch
        {
            // Ignore API failures (common on GitHub Pages if ApiBaseUrl is not configured).
        }
    }

    public async Task<AppDB?> ReloadFromLocalAsync()
    {
        Db = await _localHttp.GetFromJsonAsync<AppDB>("data/astrodb.json");
        Changed?.Invoke();
        return Db;
    }

    public void SetDb(AppDB newDb)
    {
        Db = newDb;
        Changed?.Invoke();
    }

    public async Task<string> SaveSnapshotAsync(string? label, bool setDefault)
    {
        if (Db is null) throw new InvalidOperationException("DB not loaded");

        var json = JsonSerializer.Serialize(Db, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        });

        return await _api.CreateSnapshotAsync(json, label, setDefault);
    }

    public async Task LoadSnapshotAsync(long id)
    {
        var json = await _api.GetSnapshotJsonAsync(id);
        var db = Deserialize(json);
        if (db is null) throw new InvalidOperationException("Failed to deserialize snapshot.");
        Db = db;
        Changed?.Invoke();
    }

    private static AppDB? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AppDB>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });
        }
        catch
        {
            return null;
        }
    }
}
