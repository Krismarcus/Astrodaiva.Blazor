using Astrodaiva.Data.Enums;
using Astrodaiva.Data.Models;

namespace Astrodaiva.Blazor.Integration;

public static class AspectDisplay
{
    public static string Symbol(AspectSymbol aspect) => aspect switch
    {
        AspectSymbol.Conjunction => "☌",
        AspectSymbol.Opposition => "☍",
        AspectSymbol.Trine => "△",
        AspectSymbol.Square => "□",
        AspectSymbol.Sextile => "⚹",
        _ => ""
    };

    public static List<AspectOccurrence> ForDate(AppDB db, DateTime date)
    {
        var aspects = new List<AspectOccurrence>();
        foreach (var day in db.AstroEventsDB.Where(day => day.Date.Date == date.Date))
        {
            if (day.Astronomy is not { } astronomy)
            {
                aspects.AddRange(day.PlanetEvents.Select(e => new AspectOccurrence(e.Planet1, e.Planet2, e.AspectSymbol)));
                continue;
            }

            // The editable event collection is authoritative for imported days.
            foreach (var e in astronomy.Events.Where(e => e.Type == "aspect"))
            {
                var first = Array.IndexOf(CalendarImporter.BodyIds, e.BodyId);
                var second = Array.IndexOf(CalendarImporter.BodyIds, e.TargetBodyId);
                if (first < 0 || second < 0 || !Enum.TryParse<AspectSymbol>(e.AspectId, true, out var aspect)) continue;
                aspects.Add(new((Planet)first, (Planet)second, aspect, e.AtUtc == default ? null : e.AtUtc));
            }
        }

        return aspects.Where(e => Symbol(e.AspectSymbol).Length > 0 && Enum.IsDefined(e.Planet1) && Enum.IsDefined(e.Planet2))
            // Keep separate occurrences of the same aspect; untimed legacy entries follow exact events.
            .DistinctBy(e => (e.Planet1, e.Planet2, e.AspectSymbol, e.AtUtc))
            .OrderBy(e => e.AtUtc ?? DateTimeOffset.MaxValue).ToList();
    }
}

public sealed record AspectOccurrence(Planet Planet1, Planet Planet2, AspectSymbol AspectSymbol, DateTimeOffset? AtUtc = null)
{
    public DateTimeOffset? LocalTime => AtUtc is { } at ? TimeZoneInfo.ConvertTime(at, CalendarImporter.Vilnius) : null;
}
