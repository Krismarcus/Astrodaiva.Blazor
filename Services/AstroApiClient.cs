using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Astrodaiva.Blazor.Services;

/// <summary>
/// Thin wrapper around the ASP.NET API (Astrodaiva.Api) endpoints.
/// </summary>
public class AstroApiClient
{
    private readonly HttpClient _http;
    private string? _adminToken;

    public AstroApiClient(HttpClient http) => _http = http;

    public void SetAdminToken(string? token)
    {
        _adminToken = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
    }

    /// <summary>Returns raw JSON of the default snapshot, or marks the API as unavailable.</summary>
    public async Task<DefaultSnapshotResult> TryGetDefaultSnapshotJsonAsync()
    {
        try
        {
            var resp = await _http.GetAsync("api/import/default");
            if (resp.StatusCode == HttpStatusCode.NotFound) return DefaultSnapshotResult.NoSnapshot();

            if (!resp.IsSuccessStatusCode)
                return DefaultSnapshotResult.Unavailable();

            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            return DefaultSnapshotResult.Success(json);
        }
        catch
        {
            return DefaultSnapshotResult.Unavailable();
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

    public async Task<AdminLoginResponse?> LoginAdminAsync(string password)
    {
        var resp = await _http.PostAsJsonAsync("api/auth/admin/login", new AdminLoginRequest(password));
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            return null;

        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<AdminLoginResponse>();
    }

    public async Task DeleteSnapshotAsync(long id)
    {
        using var req = CreateAdminRequest(HttpMethod.Delete, $"api/import/snapshots/{id}");
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<string> CreateSnapshotAsync(string appDbJson, string? label, bool setDefault)
    {
        // API expects a JSON body (SaveSnapshotRequest), not raw AppDB JSON.
        var payload = new SaveSnapshotRequest(label, setDefault, appDbJson);

        // Note: keep this relative (BaseAddress points to the API host)
        using var req = CreateAdminRequest(HttpMethod.Post, "api/import/full-sync");
        req.Content = JsonContent.Create(payload);
        var resp = await _http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        resp.EnsureSuccessStatusCode();
        return body;
    }

    public async Task SetDefaultSnapshotAsync(long id)
    {
        using var req = CreateAdminRequest(HttpMethod.Post, $"api/import/snapshots/{id}/set-default");
        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateAdminRequest(HttpMethod method, string requestUri)
    {
        if (string.IsNullOrWhiteSpace(_adminToken))
            throw new InvalidOperationException("Admin login is required for this action.");

        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        return request;
    }

    public record DefaultSnapshotResult(string? Json, bool IsServerUnavailable)
    {
        public static DefaultSnapshotResult Success(string json) => new(json, false);
        public static DefaultSnapshotResult NoSnapshot() => new(null, false);
        public static DefaultSnapshotResult Unavailable() => new(null, true);
    }

    public record AdminLoginRequest(string Password);
    public record AdminLoginResponse(string Token, DateTimeOffset ExpiresUtc);
    public record SaveSnapshotRequest(string? Label, bool SetDefault, string Json);

    public record SnapshotItemDto(long Id, DateTime CreatedUtc, string? Label, bool IsDefault, int SizeBytes);
}
