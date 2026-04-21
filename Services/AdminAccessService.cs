using Microsoft.JSInterop;

namespace Astrodaiva.Blazor.Services;

public sealed class AdminAccessService
{
    private const string StorageKey = "astrodaiva.admin.unlocked";
    private readonly IJSRuntime _js;

    public AdminAccessService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<bool> IsUnlockedAsync()
    {
        try
        {
            return await _js.InvokeAsync<bool>("adminAccess.isUnlocked", StorageKey);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnlockAsync(string password)
    {
        try
        {
            return await _js.InvokeAsync<bool>("adminAccess.unlock", StorageKey, password);
        }
        catch
        {
            return false;
        }
    }

    public async Task LockAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("adminAccess.lock", StorageKey);
        }
        catch
        {
        }
    }
}
