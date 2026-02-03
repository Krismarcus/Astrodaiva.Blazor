using System.Text.Json;
using System.Net.Http.Json;
using Astrodaiva.Data.Models;

namespace Astrodaiva.Blazor.Services;

/// <summary>
/// Single source of truth for the AppDB JSON in the Blazor client.
///
/// Load order:
/// 1) Default snapshot from API (if present)
/// 2) Fallback to local wwwroot/data/astrodb.json
/// </summary>
public class AstroDbStore
{
    private readonly HttpClient _localHttp;
    private readonly AstroApiClient _api;

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

        // 1) Try API default snapshot
        var json = await _api.TryGetDefaultSnapshotJsonAsync();
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

        // 2) Fallback to local JSON
        try
        {
            var localDb = await _localHttp.GetFromJsonAsync<AppDB>("data/astrodb.json");
            Db = localDb;
        }
        catch
        {
            Db = null;
        }

        Changed?.Invoke();
        return Db;
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
