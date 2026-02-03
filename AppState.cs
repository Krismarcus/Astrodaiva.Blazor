namespace Astrodaiva.Blazor;
public class AppState
{
    // TODO: Port state from MAUI (selected date, events cache, etc.)
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}