using System.Text.Json;
using Astrodaiva.Data.Models;

namespace Astrodaiva.Blazor.Services;

/// <summary>
/// Single source of truth for the AppDB JSON in the Blazor client.
///
/// Load order:
/// 1) Default snapshot from API (if present and quick)
/// 2) Local wwwroot/data/astrodb.json fallback (fast, always available on GH Pages)
/// </summary>
public class AstroDbStore
{
    private readonly HttpClient _localHttp;
    private readonly AstroApiClient _api;

    private bool _apiRefreshStarted;
    private bool _serverUnavailableNotified;
    private Task? _apiRefreshTask;
    private AppDB? _localFallbackDb;

    public AstroDbStore(HttpClient localHttp, AstroApiClient api)
    {
        _localHttp = localHttp;
        _api = api;
    }

    public AppDB? Db { get; private set; }
    public bool IsLoaded => Db is not null;

    public event Action? Changed;
    public event Action? ServerUnavailable;
    public event Action? ServerAvailable;

    public async Task<AppDB?> EnsureLoadedAsync()
    {
        if (Db is not null) return Db;

        // DB-first startup:
        // 1) Try API default snapshot first, matching the themed loading bar duration.
        // 2) If the API is unreachable/slow after that, fall back to local JSON and show retry UI.
        //
        // NOTE: App.razor waits for this method before showing Router, so keep it fast.

        const int apiTimeoutMs = 5000;
        var apiTask = _api.TryGetDefaultSnapshotJsonAsync();

        try
        {
            var completed = await Task.WhenAny(apiTask, Task.Delay(apiTimeoutMs));
            if (completed == apiTask)
            {
                var result = await apiTask; // may be empty when no snapshot exists
                if (result.IsServerUnavailable)
                    NotifyServerUnavailable();

                if (await TryApplyApiSnapshotAsync(result.Json))
                    return Db;

                // No snapshot (or failed to deserialize) -> fall through to local JSON.
            }
            else
            {
                // Timed out: show local JSON now, but keep the first API request alive.
                // Render can cold-start slowly; when it eventually responds, update the app automatically.
                ContinueApiTaskInBackground(apiTask);
                NotifyServerUnavailable();
            }
        }
        catch
        {
            NotifyServerUnavailable();
        }

        // Fallback: local JSON. Keep this finite too; on some mobile browsers a
        // stalled fetch can otherwise leave the app on the boot screen forever.
        try
        {
            Db = await LoadLocalFallbackAsync();
        }
        catch
        {
            Db = null;
        }

        Changed?.Invoke();
        return Db;
    }

    private void ContinueApiTaskInBackground(Task<AstroApiClient.DefaultSnapshotResult> apiTask)
    {
        if (_apiRefreshStarted) return;
        _apiRefreshStarted = true;

        _apiRefreshTask = ApplyApiTaskWhenCompleteAsync(apiTask);
    }

    public async Task<bool> RetryDefaultSnapshotAsync()
    {
        try
        {
            var result = await _api.TryGetDefaultSnapshotJsonAsync();
            if (result.IsServerUnavailable)
            {
                NotifyServerUnavailable(force: true);
                return false;
            }

            _serverUnavailableNotified = false;
            await TryApplyApiSnapshotAsync(result.Json);
            return true;
        }
        catch
        {
            NotifyServerUnavailable(force: true);
            return false;
        }
    }

    private async Task ApplyApiTaskWhenCompleteAsync(Task<AstroApiClient.DefaultSnapshotResult> apiTask)
    {
        try
        {
            var result = await apiTask;

            if (await TryApplyApiSnapshotAsync(result.Json))
                return;

            if (result.IsServerUnavailable)
                await RetryApiInBackgroundAsync();
        }
        catch
        {
            await RetryApiInBackgroundAsync();
        }
    }

    private async Task RetryApiInBackgroundAsync()
    {
        const int backgroundAttempts = 2;

        for (var attempt = 0; attempt < backgroundAttempts; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(TimeSpan.FromSeconds(5));

            var result = await _api.TryGetDefaultSnapshotJsonAsync();

            if (await TryApplyApiSnapshotAsync(result.Json))
                return;

            if (!result.IsServerUnavailable)
                return;
        }
    }

    private async Task<bool> TryApplyApiSnapshotAsync(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        var apiDb = Deserialize(json);
        if (apiDb is null)
            return false;

        await MergeMissingEnglishInterpretationsAsync(apiDb);

        Db = apiDb;
        _serverUnavailableNotified = false;
        Changed?.Invoke();
        ServerAvailable?.Invoke();
        return true;
    }

    private async Task<AppDB?> LoadLocalFallbackAsync()
    {
        if (_localFallbackDb is not null)
            return _localFallbackDb;

        foreach (var path in LocalFallbackPaths())
        {
            var db = await TryLoadLocalFallbackPathAsync(path);
            if (db is null)
                continue;

            _localFallbackDb = db;
            return _localFallbackDb;
        }

        return null;
    }

    private static string[] LocalFallbackPaths() => new[]
    {
        "data/astrodb.json",
        "astrodb.json"
    };

    private async Task<AppDB?> TryLoadLocalFallbackPathAsync(string path)
    {
        var loadTask = LoadLocalFallbackPathCoreAsync(path);
        var completed = await Task.WhenAny(loadTask, Task.Delay(TimeSpan.FromSeconds(8)));

        if (completed != loadTask)
        {
            _ = ObserveFaultAsync(loadTask);
            return null;
        }

        try
        {
            return await loadTask;
        }
        catch
        {
            return null;
        }
    }

    private async Task<AppDB?> LoadLocalFallbackPathCoreAsync(string path)
    {
        using var response = await _localHttp.GetAsync(path);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return Deserialize(json);
    }

    private static async Task ObserveFaultAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            // The visible load path already moved on to the next fallback.
        }
    }

    private async Task MergeMissingEnglishInterpretationsAsync(AppDB db)
    {
        var local = await LoadLocalFallbackAsync();
        if (local is null || ReferenceEquals(local, db))
            return;

        if (db.PlanetInZodiacsDB is not null && local.PlanetInZodiacsDB is not null)
        {
            var localPlanetInfo = local.PlanetInZodiacsDB
                .Where(x => !string.IsNullOrWhiteSpace(x.PlanetInZodiacInfoEn))
                .ToDictionary(x => (x.Planet, x.ZodiacSign), x => x.PlanetInZodiacInfoEn);

            foreach (var item in db.PlanetInZodiacsDB)
            {
                if (string.IsNullOrWhiteSpace(item.PlanetInZodiacInfoEn) &&
                    localPlanetInfo.TryGetValue((item.Planet, item.ZodiacSign), out var english))
                {
                    item.PlanetInZodiacInfoEn = english;
                }
            }
        }

        if (db.PlanetInRetrogradeDetailsDB is not null && local.PlanetInRetrogradeDetailsDB is not null)
        {
            var localRetroInfo = local.PlanetInRetrogradeDetailsDB
                .Where(x => !string.IsNullOrWhiteSpace(x.PlanetInRetrogradeInfoEn))
                .ToDictionary(x => x.PlanetInRetrograde, x => x.PlanetInRetrogradeInfoEn);

            foreach (var item in db.PlanetInRetrogradeDetailsDB)
            {
                if (string.IsNullOrWhiteSpace(item.PlanetInRetrogradeInfoEn) &&
                    localRetroInfo.TryGetValue(item.PlanetInRetrograde, out var english))
                {
                    item.PlanetInRetrogradeInfoEn = english;
                }
            }
        }

        if (db.MoonDayDetailsDB is not null && local.MoonDayDetailsDB is not null)
        {
            var localMoonInfo = local.MoonDayDetailsDB
                .Where(x => !string.IsNullOrWhiteSpace(x.MoonDayInfoEn))
                .ToDictionary(x => x.MoonDay, x => x.MoonDayInfoEn);

            foreach (var item in db.MoonDayDetailsDB)
            {
                if (string.IsNullOrWhiteSpace(item.MoonDayInfoEn) &&
                    localMoonInfo.TryGetValue(item.MoonDay, out var english))
                {
                    item.MoonDayInfoEn = english;
                }
            }
        }
    }

    private void NotifyServerUnavailable(bool force = false)
    {
        if (_serverUnavailableNotified && !force) return;

        _serverUnavailableNotified = true;
        ServerUnavailable?.Invoke();
    }

    public async Task<AppDB?> ReloadFromLocalAsync()
    {
        _localFallbackDb = null;
        Db = await LoadLocalFallbackAsync();
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
        await MergeMissingEnglishInterpretationsAsync(db);
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
