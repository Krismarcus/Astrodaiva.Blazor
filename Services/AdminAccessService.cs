using Microsoft.JSInterop;

namespace Astrodaiva.Blazor.Services;

public sealed class AdminAccessService
{
    private const string TokenStorageKey = "astrodaiva.admin.token";
    private const string ExpiresStorageKey = "astrodaiva.admin.expiresUtc";
    private readonly IJSRuntime _js;
    private readonly AstroApiClient _api;

    public AdminAccessService(IJSRuntime js, AstroApiClient api)
    {
        _js = js;
        _api = api;
    }

    public async Task<bool> IsUnlockedAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("adminAccess.getToken", TokenStorageKey, ExpiresStorageKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                _api.SetAdminToken(null);
                return false;
            }

            _api.SetAdminToken(token);
            return true;
        }
        catch
        {
            _api.SetAdminToken(null);
            return false;
        }
    }

    public async Task<bool> UnlockAsync(string password)
    {
        try
        {
            var login = await _api.LoginAdminAsync(password);
            if (login is null)
                return false;

            _api.SetAdminToken(login.Token);
            await _js.InvokeVoidAsync("adminAccess.setToken", TokenStorageKey, ExpiresStorageKey, login.Token, login.ExpiresUtc);
            return true;
        }
        catch
        {
            _api.SetAdminToken(null);
            return false;
        }
    }

    public async Task LockAsync()
    {
        try
        {
            _api.SetAdminToken(null);
            await _js.InvokeVoidAsync("adminAccess.lock", TokenStorageKey, ExpiresStorageKey);
        }
        catch
        {
        }
    }
}
