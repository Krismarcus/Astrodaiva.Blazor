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

    public static List<PlanetEvent> ForDate(AppDB db, DateTime date)
    {
        var aspects = new List<PlanetEvent>();
        foreach (var day in db.AstroEventsDB.Where(day => day.Date.Date == date.Date))
        {
            if (day.Astronomy is not { } astronomy)
            {
                aspects.AddRange(day.PlanetEvents);
                continue;
            }

            // The editable event collection is authoritative for imported days.
            foreach (var e in astronomy.Events.Where(e => e.Type == "aspect").OrderBy(e => e.AtUtc))
            {
                var first = Array.IndexOf(CalendarImporter.BodyIds, e.BodyId);
                var second = Array.IndexOf(CalendarImporter.BodyIds, e.TargetBodyId);
                if (first < 0 || second < 0 || !Enum.TryParse<AspectSymbol>(e.AspectId, true, out var aspect)) continue;
                aspects.Add(new() { Planet1 = (Planet)first, Planet2 = (Planet)second, AspectSymbol = aspect });
            }
        }

        return aspects.Where(e => Symbol(e.AspectSymbol).Length > 0 && Enum.IsDefined(e.Planet1) && Enum.IsDefined(e.Planet2))
            .DistinctBy(e => (e.Planet1, e.Planet2, e.AspectSymbol)).ToList();
    }
}
