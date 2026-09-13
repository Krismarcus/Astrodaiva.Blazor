using System.Text.Json;
using Astrodaiva.Blazor.Integration;
using Astrodaiva.Data.Enums;
using Astrodaiva.Data.Models;

static class RetrogradeChecks
{
    public static void Run(Action<bool, string> check)
    {
        AppDB Empty() => new() { AstroEventsDB = new() };
        AstroEvent Day(DateTime date, Planet planet, bool atNoon, params ExactEvent[] stations)
        {
            var day = new AstroEvent { Date = date, Astronomy = new() { Events = stations.ToList() } };
            typeof(AstroEvent).GetProperty(CalendarImporter.PlanetFields[(int)planet])!.SetValue(day,
                new PlanetInZodiac { Planet = planet, IsRetrograde = atNoon });
            return day;
        }
        ExactEvent Station(string local, bool retrograde, Planet planet = Planet.Mercury) => new()
        {
            Type = "station", BodyId = CalendarImporter.BodyIds[(int)planet], AtUtc = DateTimeOffset.Parse(local),
            Extra = new() { ["motion"] = JsonSerializer.SerializeToElement(retrograde ? "retrograde" : "direct") }
        };

        var year = Empty();
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "retrogrades-2027.json")));
        foreach (var row in fixture.RootElement.GetProperty("days").EnumerateArray())
        {
            var day = new AstroEvent { Date = DateTime.Parse(row.GetProperty("date").GetString()!), Astronomy = new() };
            var atNoon = row.GetProperty("retrogradeAtNoon").EnumerateArray().Select(p => p.GetString()).ToHashSet();
            foreach (var planet in Enum.GetValues<Planet>().Where(p => p is >= Planet.Mercury and <= Planet.Pluto))
                typeof(AstroEvent).GetProperty(CalendarImporter.PlanetFields[(int)planet])!.SetValue(day,
                    new PlanetInZodiac { Planet = planet, IsRetrograde = atNoon.Contains(CalendarImporter.BodyIds[(int)planet]) });
            day.Astronomy.Events = row.GetProperty("stations").EnumerateArray().Select(s => Station(
                s.GetProperty("atUtc").GetString()!, s.GetProperty("motion").GetString() == "retrograde",
                (Planet)Array.IndexOf(CalendarImporter.BodyIds, s.GetProperty("bodyId").GetString()))).ToList();
            year.AstroEventsDB.Add(day);
        }
        var untouched = JsonSerializer.Serialize(year);
        var expected = new Dictionary<Planet, string>
        {
            [Planet.Mercury] = "02-09/03-03,06-10/07-04,10-07/10-28",
            [Planet.Venus] = "",
            [Planet.Mars] = "01-10/04-01",
            [Planet.Jupiter] = "01-01/04-13",
            [Planet.Saturn] = "08-09/12-24",
            [Planet.Uranus] = "01-01/02-08,09-15/12-31",
            [Planet.Neptune] = "07-10/12-15",
            [Planet.Pluto] = "05-08/10-18"
        };
        foreach (var (planet, dates) in expected)
        {
            var periods = RetrogradeDisplay.ForYear(year, 2027, planet);
            check(string.Join(',', periods.Select(p => $"{p.StartDate:MM-dd}/{p.EndDate:MM-dd}")) == dates,
                $"2027 {planet} uses correct Vilnius station dates, including year boundaries");
            foreach (var station in year.AstroEventsDB.SelectMany(d => d.Astronomy!.Events).Where(e => e.BodyId == CalendarImporter.BodyIds[(int)planet]))
            {
                var at = TimeZoneInfo.ConvertTime(station.AtUtc, CalendarImporter.Vilnius).DateTime;
                check(RetrogradeDisplay.StationState(station) == true
                        ? periods.Any(p => p.StartsAt == at && p.StartsAtStation)
                        : periods.Any(p => p.EndsAt == at && p.EndsAtStation),
                    $"{planet} boundary matches API timestamp {at:yyyy-MM-dd HH:mm:ss}");
            }
        }
        check(JsonSerializer.Serialize(year) == untouched, "retrograde rendering leaves saved noon flags and imported data untouched");

        // Both morning and evening starts/ends must be independent of the noon snapshot.
        foreach (var hour in new[] { 8, 19 })
        {
            var start = Station($"2027-02-09T{hour:00}:00:00+02:00", true);
            var end = Station($"2027-02-10T{hour:00}:00:00+02:00", false);
            var db = Empty();
            db.AstroEventsDB.Add(Day(new(2027, 2, 9), Planet.Mercury, hour < 12, start));
            db.AstroEventsDB.Add(Day(new(2027, 2, 10), Planet.Mercury, hour >= 12, end));
            var period = RetrogradeDisplay.ForYear(db, 2027, Planet.Mercury).Single();
            check(period.StartsAt == new DateTime(2027, 2, 9, hour, 0, 0) && period.EndsAt == new DateTime(2027, 2, 10, hour, 0, 0),
                $"{hour}:00 stations neither add nor subtract a calendar day");
            check(RetrogradeDisplay.ForDay(db.AstroEventsDB[0], Planet.Mercury).Single().StartsAt == period.StartsAt
                && RetrogradeDisplay.ForDay(db.AstroEventsDB[1], Planet.Mercury).Single().EndsAt == period.EndsAt,
                $"day badges include both {hour}:00 station dates independently of noon flags");
        }

        var midnight = Empty();
        midnight.AstroEventsDB.Add(Day(new(2027, 2, 9), Planet.Mercury, true, Station("2027-02-09T00:00:00+02:00", true)));
        midnight.AstroEventsDB.Add(Day(new(2027, 2, 10), Planet.Mercury, false, Station("2027-02-10T00:00:00+02:00", false)));
        var midnightPeriod = RetrogradeDisplay.ForYear(midnight, 2027, Planet.Mercury).Single();
        check(midnightPeriod.StartDate == new DateTime(2027, 2, 9) && midnightPeriod.EndDate == new DateTime(2027, 2, 10)
            && midnightPeriod.EndsAtStation && (midnightPeriod.EndsAt - midnightPeriod.StartsAt).TotalDays == 1,
            "midnight stations have exact labels without adding a full day to the bar");
        check(RetrogradeDisplay.ForDay(midnight.AstroEventsDB[0], Planet.Mercury).Count == 1
            && RetrogradeDisplay.ForDay(midnight.AstroEventsDB[1], Planet.Mercury).Count == 0,
            "day badge appears for a midnight start but not a midnight direct station");

        var shortPeriod = Empty();
        shortPeriod.AstroEventsDB.Add(Day(new(2027, 2, 9), Planet.Mercury, false,
            Station("2027-02-09T17:00:00+02:00", true), Station("2027-02-09T19:00:00+02:00", false)));
        check((RetrogradeDisplay.ForYear(shortPeriod, 2027, Planet.Mercury).Single().EndsAt
            - RetrogradeDisplay.ForYear(shortPeriod, 2027, Planet.Mercury).Single().StartsAt).TotalHours == 2,
            "multiple same-day stations produce a partial-day period even when noon is direct");

        var manual = Empty();
        for (var d = 8; d <= 11; d++)
        {
            var day = Day(new(2027, 2, d), Planet.Mercury, d is 9 or 10);
            day.Astronomy = null;
            manual.AstroEventsDB.Add(day);
        }
        var legacy = RetrogradeDisplay.ForYear(manual, 2027, Planet.Mercury).Single();
        check(legacy.StartDate == new DateTime(2027, 2, 9) && legacy.EndDate == new DateTime(2027, 2, 10)
            && (legacy.EndsAt - legacy.StartsAt).TotalDays == 2,
            "manual whole-day retrograde marks preserve inclusive labels and width");

        var overridden = CalendarImporter.Clone(shortPeriod);
        overridden.AstroEventsDB[0].Astronomy!.Overrides.Add("MercuryInZodiac");
        check(RetrogradeDisplay.ForYear(overridden, 2027, Planet.Mercury).Count == 0,
            "manual planet overrides take precedence over imported stations");
        check(RetrogradeDisplay.ForDay(overridden.AstroEventsDB[0], Planet.Mercury).Count == 0
            && RetrogradeDisplay.ForDay(manual.AstroEventsDB[1], Planet.Mercury).Count == 1
            && RetrogradeDisplay.ForDay(manual.AstroEventsDB[0], Planet.Mercury).Count == 0,
            "day badges respect manual retrograde marks and imported-data overrides");
        shortPeriod.AstroEventsDB[0].Astronomy!.Source.Events = CalendarImporter.Clone(shortPeriod.AstroEventsDB[0].Astronomy!.Events);
        shortPeriod.AstroEventsDB[0].Astronomy!.Events.Clear();
        check(RetrogradeDisplay.ForYear(shortPeriod, 2027, Planet.Mercury).Count == 0,
            "deleted editable stations cannot reappear from original API source metadata");

        var gap = Empty();
        gap.AstroEventsDB.Add(Day(new(2028, 2, 28), Planet.Mercury, true));
        gap.AstroEventsDB.Add(Day(new(2028, 3, 1), Planet.Mercury, true));
        check(RetrogradeDisplay.ForYear(gap, 2028, Planet.Mercury).Count == 2,
            "unknown dates are not silently filled with retrograde motion");
        gap.AstroEventsDB.Add(Day(new(2028, 2, 29), Planet.Mercury, true));
        var leap = RetrogradeDisplay.ForYear(gap, 2028, Planet.Mercury).Single();
        check((leap.EndsAt - leap.StartsAt).TotalDays == 3 && leap.EndDate == new DateTime(2028, 3, 1),
            "leap days and unsorted rows merge into the correct duration");

        check(RetrogradeDisplay.StationState(new ExactEvent { Type = "station", Direction = "direct" }) == false
            && RetrogradeDisplay.StationState(new ExactEvent { Type = "station", Direction = "unknown" }) is null,
            "station direction fallback is shared with exact-event descriptions");
    }
}
