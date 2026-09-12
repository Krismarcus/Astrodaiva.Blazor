using Microsoft.JSInterop;
using Astrodaiva.Blazor.Integration;

namespace Astrodaiva.Blazor.Services;

public sealed class VisitorTimeZone(IJSRuntime js)
{
    public string Selection { get; private set; } = "auto";
    public string DetectedZone { get; private set; } = "Europe/Vilnius";
    public TimeZoneInfo Zone { get; private set; } = CalendarImporter.Vilnius;
    public string[] AvailableZones { get; private set; } = new[] { "Europe/Vilnius" };
    public event Action? Changed;
    private bool initialized;

    public async Task InitializeAsync()
    {
        if (initialized) return;
        try
        {
            var settings = await js.InvokeAsync<ZoneSettings>("astroTimeZone.read");
            DetectedZone = settings.Detected;
            AvailableZones = settings.Zones.Append("Europe/Vilnius").Append(DetectedZone).Distinct().Order().ToArray();
            Apply(settings.Selected);
        }
        catch { Apply("auto"); }
        initialized = true;
    }

    public async Task SelectAsync(string selection)
    {
        Apply(selection);
        try { await js.InvokeVoidAsync("astroTimeZone.save", Selection); } catch { }
    }

    private void Apply(string selection)
    {
        try
        {
            Zone = TimeZoneInfo.FindSystemTimeZoneById(selection == "auto" ? DetectedZone : selection);
            Selection = selection;
        }
        catch { Zone = CalendarImporter.Vilnius; Selection = "Europe/Vilnius"; }
        Changed?.Invoke();
    }
    public DateTimeOffset Convert(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Zone);
    public sealed record ZoneSettings(string Detected, string Selected, string[] Zones);
}
