using Astrodaiva.Data.Models;

namespace Astrodaiva.Blazor.Integration;

public static class CalendarDisplay
{
    public static IEnumerable<AstroEvent> SourceDays(AppDB db) => db.AstroEventsDB.Where(d => d.Astronomy is not null)
        .Concat(db.AstronomyContext).GroupBy(d => d.Date.Date).Select(g => g.First());

    public static (DateTimeOffset Start, DateTimeOffset End) Bounds(DateTime day, TimeZoneInfo zone)
        => (StartOfDay(day.Date, zone), StartOfDay(day.Date.AddDays(1), zone));

    private static DateTimeOffset StartOfDay(DateTime date, TimeZoneInfo zone)
    {
        var local = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
        // Some zones move clocks at midnight; use the first existing instant of the date.
        while (zone.IsInvalidTime(local)) local = local.AddMinutes(1);
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    public static List<LunarSegment> SegmentsFor(AppDB db, DateTime date, TimeZoneInfo zone)
    {
        var (start, end) = Bounds(date, zone);
        var result = new List<LunarSegment>();
        foreach (var segment in SourceDays(db).SelectMany(d => d.Astronomy!.Segments)
                     .Where(s => s.StartsAtUtc < end && s.EndsAtUtc > start).OrderBy(s => s.StartsAtUtc))
        {
            var clipped = new LunarSegment
            {
                LunarDayNumber = segment.LunarDayNumber,
                StartsAtUtc = segment.StartsAtUtc < start ? start : segment.StartsAtUtc,
                EndsAtUtc = segment.EndsAtUtc > end ? end : segment.EndsAtUtc
            };
            if (result.Count > 0 && result[^1].LunarDayNumber == clipped.LunarDayNumber && result[^1].EndsAtUtc == clipped.StartsAtUtc)
                result[^1].EndsAtUtc = clipped.EndsAtUtc;
            else result.Add(clipped);
        }
        return result;
    }

    public static List<ExactEvent> EventsFor(AppDB db, DateTime date, TimeZoneInfo zone)
    {
        var (start, end) = Bounds(date, zone);
        return SourceDays(db).SelectMany(EffectiveEvents).Where(e => e.AtUtc >= start && e.AtUtc < end)
            .GroupBy(e => e.EventId).Select(g => g.First()).OrderBy(e => e.AtUtc).ToList();
    }

    public static IEnumerable<ExactEvent> EffectiveEvents(AstroEvent day)
    {
        if (day.Astronomy is not { } a) yield break;
        foreach (var e in a.Events)
        {
            if (e.Type == "lunar-day-start" && a.Overrides.Contains("MoonDay")) continue;
            if (e.Type == "moon-phase" && a.Overrides.Contains("MoonPhase")) continue;
            if (e.Type == "solar-eclipse" && !day.SunEclipse || e.Type == "lunar-eclipse" && !day.MoonEclipse) continue;
            var planet = Array.IndexOf(CalendarImporter.BodyIds, e.BodyId);
            if ((e.Type is "ingress" or "station") && planet >= 0 && a.Overrides.Contains(CalendarImporter.PlanetFields[planet])) continue;
            yield return e;
        }
        if (a.Overrides.Contains("MoonDay"))
            foreach (var segment in a.Segments.Skip(1))
                yield return new ExactEvent { EventId = $"manual-lunar-{day.Date:yyyyMMdd}-{segment.StartsAtUtc.UtcTicks}", Type = "lunar-day-start", AtUtc = segment.StartsAtUtc, LunarDayNumber = segment.LunarDayNumber };
        for (var i = 0; i < CalendarImporter.PlanetFields.Length; i++)
        {
            if (!a.Overrides.Contains(CalendarImporter.PlanetFields[i])) continue;
            var planet = (PlanetInZodiac?)typeof(AstroEvent).GetProperty(CalendarImporter.PlanetFields[i])!.GetValue(day);
            if (planet is not { IsZodiacTransitioning: true } || planet.TransitionTime == default) continue;
            var local = DateTime.SpecifyKind(planet.TransitionTime, DateTimeKind.Unspecified);
            if (CalendarImporter.Vilnius.IsInvalidTime(local) || CalendarImporter.Vilnius.IsAmbiguousTime(local)) continue;
            yield return new ExactEvent
            {
                EventId = $"manual-ingress-{day.Date:yyyyMMdd}-{i}", Type = "ingress", BodyId = CalendarImporter.BodyIds[i],
                AtUtc = new DateTimeOffset(local, CalendarImporter.Vilnius.GetUtcOffset(local)),
                Extra = new() { ["toLegacySignId"] = System.Text.Json.JsonSerializer.SerializeToElement((int)planet.NewZodiacSign) }
            };
        }
    }
}
