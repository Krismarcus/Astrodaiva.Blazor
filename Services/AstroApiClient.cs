using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Astrodaiva.Blazor.Integration;

namespace Astrodaiva.Blazor.Services;

/// <summary>
/// Thin wrapper around the ASP.NET API (Astrodaiva.Api) endpoints.
/// </summary>
public class AstroApiClient
{
    private readonly HttpClient _http;
    private string? _adminToken;
    public string? PublishedRevision { get; private set; }
    public void AcceptPublishedRevision(string? revision) => PublishedRevision = revision;

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
            using var resp = await _http.GetAsync("api/import/default");
            var revision = resp.Headers.ETag?.Tag.Trim('"');
            if (resp.StatusCode == HttpStatusCode.NotFound) return DefaultSnapshotResult.NoSnapshot(revision);

            if (!resp.IsSuccessStatusCode)
                return DefaultSnapshotResult.Unavailable();

            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            return DefaultSnapshotResult.Success(json, revision);
        }
        catch
        {
            return DefaultSnapshotResult.Unavailable();
        }
    }

    public async Task<List<SnapshotItemDto>> ListSnapshotsAsync(int take = 80)
    {
        take = Math.Clamp(take, 1, 200);
        using var request = CreateAdminRequest(HttpMethod.Get, $"api/import/snapshots?take={take}");
        using var response = await _http.SendAsync(request);
        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<List<SnapshotItemDto>>() ?? new();
    }

    public async Task<DefaultSnapshotResult> GetAdminDefaultSnapshotAsync()
    {
        using var request = CreateAdminRequest(HttpMethod.Get, "api/import/admin-default");
        using var response = await _http.SendAsync(request);
        var revision = response.Headers.ETag?.Tag.Trim('"');
        if (response.StatusCode == HttpStatusCode.NotFound && revision == "none")
            return DefaultSnapshotResult.NoSnapshot(revision);
        await EnsureSuccess(response);
        return DefaultSnapshotResult.Success(await response.Content.ReadAsStringAsync(), revision);
    }

    public async Task<string> GetSnapshotJsonAsync(long id)
    {
        using var request = CreateAdminRequest(HttpMethod.Get, $"api/import/snapshots/{id}");
        using var response = await _http.SendAsync(request);
        await EnsureSuccess(response);
        return await response.Content.ReadAsStringAsync();
    }

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
        await EnsureSuccess(resp);
    }

    public async Task<string> CreateSnapshotAsync(string appDbJson, string? label, bool setDefault)
    {
        // API expects a JSON body (SaveSnapshotRequest), not raw AppDB JSON.
        var payload = new SaveSnapshotRequest(label, setDefault, appDbJson, PublishedRevision);

        // Note: keep this relative (BaseAddress points to the API host)
        using var req = CreateAdminRequest(HttpMethod.Post, "api/import/full-sync");
        req.Content = JsonContent.Create(payload);
        var resp = await _http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        await EnsureSuccess(resp);
        using var result = JsonDocument.Parse(body);
        if (setDefault && result.RootElement.TryGetProperty("revision", out var revision)) PublishedRevision = revision.GetString();
        return body;
    }

    public async Task SetDefaultSnapshotAsync(long id)
    {
        using var req = CreateAdminRequest(HttpMethod.Post, $"api/import/snapshots/{id}/set-default");
        req.Content = JsonContent.Create(new { baseRevision = PublishedRevision });
        using var resp = await _http.SendAsync(req);
        await EnsureSuccess(resp);
        using var result = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        PublishedRevision = result.RootElement.GetProperty("revision").GetString();
    }

    public async Task<CelestialCalendar> ImportMonthAsync(int year, int month, CancellationToken cancellationToken)
    {
        using var request = CreateAdminRequest(HttpMethod.Post, "api/astronomy/month");
        request.Content = JsonContent.Create(new { year, month });
        // A month calculation may outlast the ordinary startup/API timeout.
        using var client = new HttpClient { BaseAddress = _http.BaseAddress, Timeout = TimeSpan.FromMinutes(4) };
        using var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<CelestialCalendar>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The calculation service returned an empty calendar.");
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var message = $"Request failed ({(int)response.StatusCode}).";
        try
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (body.RootElement.TryGetProperty("message", out var detail)) message = detail.GetString() ?? message;
        }
        catch (JsonException) { }
        if (response.StatusCode == HttpStatusCode.TooManyRequests && response.Headers.RetryAfter is { } retry)
            message += $" Retry after: {retry}.";
        throw new InvalidOperationException(message);
    }

    private HttpRequestMessage CreateAdminRequest(HttpMethod method, string requestUri)
    {
        if (string.IsNullOrWhiteSpace(_adminToken))
            throw new InvalidOperationException("Admin login is required for this action.");

        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        return request;
    }

    public record DefaultSnapshotResult(string? Json, bool IsServerUnavailable, string? Revision = null)
    {
        public static DefaultSnapshotResult Success(string json, string? revision) => new(json, false, revision);
        public static DefaultSnapshotResult NoSnapshot(string? revision = null) => new(null, false, revision);
        public static DefaultSnapshotResult Unavailable() => new(null, true);
    }

    public record AdminLoginRequest(string Password);
    public record AdminLoginResponse(string Token, DateTimeOffset ExpiresUtc);
    public record SaveSnapshotRequest(string? Label, bool SetDefault, string Json, string? BaseRevision);

    public record SnapshotItemDto(long Id, DateTime CreatedUtc, string? Label, bool IsDefault, int SizeBytes);
}
