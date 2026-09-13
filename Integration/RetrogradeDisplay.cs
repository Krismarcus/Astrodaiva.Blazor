using System.Text.Json;
using Astrodaiva.Data.Enums;
using Astrodaiva.Data.Models;

namespace Astrodaiva.Blazor.Integration;

public static class RetrogradeDisplay
{
    public static bool? StationState(ExactEvent e)
    {
        if (e.Type != "station") return null;
        var motion = e.Direction;
        if (e.Extra?.TryGetValue("motion", out var value) == true && value.ValueKind == JsonValueKind.String)
            motion = value.GetString();
        return motion switch { "retrograde" => true, "direct" => false, _ => null };
    }

    public static List<RetrogradePeriod> ForYear(AppDB db, int year, Planet planet)
        => BuildPeriods(db.AstroEventsDB.Where(d => d.Date.Year == year), planet);

    public static List<RetrogradePeriod> ForDay(AstroEvent day, Planet planet)
        => BuildPeriods(new[] { day }, planet);

    private static List<RetrogradePeriod> BuildPeriods(IEnumerable<AstroEvent> days, Planet planet)
    {
        var periods = new List<RetrogradePeriod>();
        if (planet is < Planet.Mercury or > Planet.Pluto) return periods;
        var bodyId = CalendarImporter.BodyIds[(int)planet];

        foreach (var day in days.OrderBy(d => d.Date))
        {
            var start = day.Date.Date;
            var end = start.AddDays(1);
            var stations = CalendarDisplay.EffectiveEvents(day)
                .Where(e => e.BodyId == bodyId && StationState(e).HasValue)
                .Select(e => (At: TimeZoneInfo.ConvertTime(e.AtUtc, CalendarImporter.Vilnius).DateTime,
                    Retrograde: StationState(e)!.Value))
                .Where(e => e.At >= start && e.At < end).Distinct().OrderBy(e => e.At).ToList();

            // API flags describe local noon, not the whole date. A station tells us
            // both the motion before it and the exact instant that motion changes.
            bool? retrograde = stations.Count > 0 ? !stations[0].Retrograde : SnapshotState(day, planet);
            var cursor = start;
            var startsAtStation = false;
            foreach (var station in stations)
            {
                if (retrograde == true)
                    Append(cursor, station.At, startsAtStation, !station.Retrograde);
                cursor = station.At;
                retrograde = station.Retrograde;
                startsAtStation = station.Retrograde;
            }
            if (retrograde == true) Append(cursor, end, startsAtStation, false);
        }
        return periods;

        void Append(DateTime start, DateTime end, bool startsAtStation, bool endsAtStation)
        {
            if (end < start) return;
            // Merge adjacent civil dates, but never bridge missing calendar data.
            if (periods.Count > 0 && periods[^1].EndsAt == start)
                periods[^1] = periods[^1] with { EndsAt = end, EndsAtStation = endsAtStation };
            else if (end > start)
                periods.Add(new(start, end, startsAtStation, endsAtStation));
        }
    }

    private static bool? SnapshotState(AstroEvent day, Planet planet) => planet switch
    {
        Planet.Mercury => day.MercuryInZodiac?.IsRetrograde,
        Planet.Venus => day.VenusInZodiac?.IsRetrograde,
        Planet.Mars => day.MarsInZodiac?.IsRetrograde,
        Planet.Jupiter => day.JupiterInZodiac?.IsRetrograde,
        Planet.Saturn => day.SaturnInZodiac?.IsRetrograde,
        Planet.Uranus => day.UranusInZodiac?.IsRetrograde,
        Planet.Neptune => day.NeptuneInZodiac?.IsRetrograde,
        Planet.Pluto => day.PlutoInZodiac?.IsRetrograde,
        _ => null
    };
}

// EndsAt is exclusive for geometry. Exact station labels use the station's date;
// legacy whole-day labels use the last included date, preserving manual calendars.
public sealed record RetrogradePeriod(DateTime StartsAt, DateTime EndsAt, bool StartsAtStation, bool EndsAtStation)
{
    public DateTime StartDate => StartsAt.Date;
    public DateTime EndDate => EndsAtStation ? EndsAt.Date : EndsAt.AddTicks(-1).Date;
}
