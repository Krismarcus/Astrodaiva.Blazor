using System.Text.Json;
using Astrodaiva.Blazor.Integration;
using Astrodaiva.Data.Enums;
using Astrodaiva.Data.Models;

static class AspectChecks
{
    public static void Run(Action<bool, string> check)
    {
        ExactEvent Aspect(string at, string target = "venus") => new()
        {
            Type = "aspect", BodyId = "moon", TargetBodyId = target, AspectId = "trine",
            AtUtc = DateTimeOffset.Parse(at)
        };

        var date = new DateTime(2026, 9, 15);
        var early = Aspect("2026-09-14T23:14:00Z");
        var late = Aspect("2026-09-15T18:36:00Z");
        var sameTime = Aspect("2026-09-15T18:36:00Z", "saturn");
        var db = new AppDB { AstroEventsDB = new()
        {
            new() { Date = date, Astronomy = new() { Events = new() { late, early, sameTime, late } } },
            new() { Date = date, PlanetEvents = new()
            {
                new() { Planet1 = Planet.Sun, Planet2 = Planet.Mars, AspectSymbol = AspectSymbol.Square },
                new() { Planet1 = Planet.Sun, Planet2 = Planet.Mars, AspectSymbol = AspectSymbol.Square }
            } }
        } };
        var before = JsonSerializer.Serialize(db);
        var aspects = AspectDisplay.ForDate(db, date);
        check(aspects.Count == 4 && aspects[0].AtUtc == early.AtUtc && aspects[1].AtUtc == late.AtUtc
            && aspects[2].Planet2 == Planet.Saturn,
            "aspect timeline orders exact instants, keeps repeated and simultaneous relationships, and removes duplicate occurrences");
        check(aspects[0].LocalTime == DateTimeOffset.Parse("2026-09-15T02:14:00+03:00")
            && aspects[0].LocalTime!.Value.Hour == 2 && aspects[1].LocalTime!.Value.Hour == 21,
            "aspect times use Vilnius civil time, including an event whose UTC date is the previous day");
        check(aspects[^1].AtUtc is null && aspects[^1].LocalTime is null && aspects[^1].Planet1 == Planet.Sun,
            "legacy aspects follow timed events without an invented midnight time");
        check(JsonSerializer.Serialize(db) == before, "aspect rendering leaves source order, data, and legacy projections unchanged");

        db.AstroEventsDB[0].Astronomy!.Events[0].AtUtc = DateTimeOffset.Parse("2026-09-14T22:00:00Z");
        check(AspectDisplay.ForDate(db, date)[0].LocalTime!.Value.Hour == 1,
            "an edited exact time updates both the aspect order and displayed local time");

        var autumn = new DateTime(2026, 10, 25);
        db.AstroEventsDB.Add(new() { Date = autumn, Astronomy = new() { Events = new()
        {
            Aspect("2026-10-25T01:15:00Z"), Aspect("2026-10-25T00:45:00Z")
        } } });
        var clockChange = AspectDisplay.ForDate(db, autumn);
        check(clockChange[0].LocalTime!.Value.ToString("HH:mm zzz") == "03:45 +03:00"
            && clockChange[1].LocalTime!.Value.ToString("HH:mm zzz") == "03:15 +02:00",
            "the repeated autumn hour follows actual chronology and retains the correct UTC offset");
    }
}
