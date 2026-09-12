using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Astrodaiva.Data.Enums;
using Astrodaiva.Data.Models;

namespace Astrodaiva.Blazor.Integration;

public static class CalendarImporter
{
    public static readonly string[] PlanetFields = { "SunInZodiac", "MoonInZodiac", "MercuryInZodiac", "VenusInZodiac", "MarsInZodiac", "JupiterInZodiac", "SaturnInZodiac", "UranusInZodiac", "NeptuneInZodiac", "PlutoInZodiac", "SelenaInZodiac", "LilithInZodiac", "RahuInZodiac", "KetuInZodiac" };
    public static readonly string[] Groups = new[] { "MoonDay", "MoonPhase", "SunEclipse", "MoonEclipse", "PlanetEvents" }.Concat(PlanetFields).ToArray();
    public static readonly string[] BodyIds = { "sun", "moon", "mercury", "venus", "mars", "jupiter", "saturn", "uranus", "neptune", "pluto", "selena", "lilith", "true-north-lunar-node", "true-south-lunar-node" };
    public static readonly TimeZoneInfo Vilnius = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vilnius");
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value), JsonOptions)!;
    private static PropertyInfo Property(string group) => typeof(AstroEvent).GetProperty(group)!;
    private static bool Equal(JsonElement a, JsonElement b) => JsonNode.DeepEquals(JsonNode.Parse(a.GetRawText()), JsonNode.Parse(b.GetRawText()));
    public static DateTimeOffset Round(DateTimeOffset value) => new((long)Math.Round((double)value.UtcTicks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond, TimeSpan.Zero);

    public static JsonElement Capture(AstroEvent day, string group) => group switch
    {
        "MoonDay" => JsonSerializer.SerializeToElement(new { Legacy = day.MoonDay, Segments = day.Astronomy?.Segments }),
        "PlanetEvents" => JsonSerializer.SerializeToElement(new { Legacy = day.PlanetEvents, Events = day.Astronomy?.Events }),
        _ => JsonSerializer.SerializeToElement(Property(group).GetValue(day))
    };

    public static void DetectOverrides(AstroEvent day)
    {
        if (day.Astronomy is not { } metadata) return;
        foreach (var group in Groups)
            if (metadata.Baseline.TryGetValue(group, out var baseline) && !Equal(Capture(day, group), baseline))
                metadata.Overrides.Add(group);
    }

    public static ImportPreview Preview(AppDB current, CelestialCalendar response, int year, int month, bool resetOverrides)
    {
        ValidateResponse(response, year, month);
        var draft = Clone(current);
        draft.AstroEventsDB ??= new();
        var changes = new List<ImportChange>();
        foreach (var source in response.AstronomyDays.OrderBy(d => d.Date))
        {
            var candidate = Map(source, response);
            if (source.Date.Year != year || source.Date.Month != month)
            {
                candidate.Astronomy!.IsBoundaryContext = true;
                draft.AstronomyContext.RemoveAll(d => d.Date.Date == candidate.Date.Date);
                draft.AstronomyContext.Add(candidate);
                continue;
            }
            var existing = draft.AstroEventsDB.SingleOrDefault(d => d.Date.Date == candidate.Date.Date);
            if (existing is null)
            {
                draft.AstroEventsDB.Add(candidate);
                changes.Add(new(source.Date, true, Groups.ToList(), new()));
                continue;
            }
            DetectOverrides(existing);
            var changed = new List<string>();
            var preserved = new List<string>();
            var oldMetadata = existing.Astronomy;
            foreach (var group in Groups)
            {
                if (!resetOverrides && oldMetadata?.Overrides.Contains(group) == true)
                {
                    Property(group).SetValue(candidate, Property(group).GetValue(existing));
                    if (group == "MoonDay") candidate.Astronomy!.Segments = Clone(oldMetadata.Segments);
                    if (group == "PlanetEvents") candidate.Astronomy!.Events = Clone(oldMetadata.Events);
                    candidate.Astronomy!.Overrides.Add(group);
                    preserved.Add(group);
                }
                else if (!Equal(Capture(existing, group), Capture(candidate, group))) changed.Add(group);
            }
            // Replace only astronomy. Preserve all editorial fields, text libraries and dates outside the month.
            foreach (var group in Groups) Property(group).SetValue(existing, Property(group).GetValue(candidate));
            existing.Astronomy = candidate.Astronomy;
            changes.Add(new(source.Date, false, changed, preserved));
        }
        draft.AstroEventsDB = new(draft.AstroEventsDB.OrderBy(d => d.Date));
        draft.AstronomyContext.RemoveAll(c => draft.AstroEventsDB.Any(d => d.Date == c.Date && d.Astronomy is not null));
        return new(draft, changes, response.Notices);
    }

    public static void ValidateResponse(CelestialCalendar response, int year, int month)
    {
        if (response.SchemaVersion != "1.0" || response.Location.TimeZoneId != "Europe/Vilnius")
            throw new InvalidOperationException("Unsupported calculation profile or location.");
        var start = new DateOnly(year, month, 1).AddDays(-2);
        var end = new DateOnly(year, month, 1).AddMonths(1).AddDays(1);
        if (!response.AstronomyDays.Select(d => d.Date).Order().SequenceEqual(Enumerable.Range(0, end.DayNumber - start.DayNumber + 1).Select(start.AddDays)))
            throw new InvalidOperationException("The import is missing dates or contains duplicate dates.");
        foreach (var day in response.AstronomyDays)
        {
            if (!day.Planets.Select(p => p.LegacyPlanetId).Order().SequenceEqual(Enumerable.Range(0, 14)) ||
                day.Planets.Any(p => p.LegacyZodiacSignId is < 1 or > 12 || p.BodyId != BodyIds[p.LegacyPlanetId] ||
                    p.Transitions.Any(t => t.FromLegacySignId is < 1 or > 12 || t.ToLegacySignId is < 1 or > 12)))
                throw new InvalidOperationException($"Unexpected planet or zodiac identifiers on {day.Date}.");
            ValidateSegments(day.LunarDaySegments, day.StartsAtUtc, day.EndsAtUtc);
            if (day.Events.Any(e => string.IsNullOrWhiteSpace(e.EventId) || e.AtUtc < day.StartsAtUtc || e.AtUtc >= day.EndsAtUtc) ||
                day.Events.Select(e => e.EventId).Distinct().Count() != day.Events.Count)
                throw new InvalidOperationException($"Invalid or duplicate exact events on {day.Date}.");
        }
    }

    public static void ValidateSegments(IReadOnlyList<LunarSegment> segments, DateTimeOffset start, DateTimeOffset end)
    {
        if (segments.Count == 0 || Round(segments[0].StartsAtUtc) != Round(start) || Round(segments[^1].EndsAtUtc) != Round(end))
            throw new InvalidOperationException("Lunar segments must cover the complete calculation date.");
        for (var i = 0; i < segments.Count; i++)
            if (segments[i].LunarDayNumber is < 1 or > 30 || segments[i].EndsAtUtc <= segments[i].StartsAtUtc ||
                (i > 0 && Round(segments[i - 1].EndsAtUtc) != Round(segments[i].StartsAtUtc)))
                throw new InvalidOperationException("Lunar segments must be ordered, continuous and numbered 1–30.");
    }

    public static void ValidateDraft(AppDB db)
    {
        foreach (var day in db.AstroEventsDB.Where(d => d.Astronomy is not null))
        {
            var a = day.Astronomy!;
            ValidateSegments(a.Segments, a.Source.StartsAtUtc, a.Source.EndsAtUtc);
            if (a.Events.Any(e => e.AtUtc < a.Source.StartsAtUtc || e.AtUtc >= a.Source.EndsAtUtc))
                throw new InvalidOperationException($"An exact event on {day.Date:yyyy-MM-dd} is outside its Vilnius date.");
        }
    }

    private static AstroEvent Map(CalculatedDay source, CelestialCalendar response)
    {
        var day = new AstroEvent
        {
            Date = source.Date.ToDateTime(TimeOnly.MinValue), MoonPhase = source.MoonPhase.LegacyQuarterCode,
            SunEclipse = source.SolarEclipse, MoonEclipse = source.LunarEclipse,
            Barber = ActivityQuality.None, Beauty = ActivityQuality.None, Buystuff = ActivityQuality.None,
            Contracts = ActivityQuality.None, ImportantTasks = ActivityQuality.None, Gardening = ActivityQuality.None,
            Love = ActivityQuality.None, Meetings = ActivityQuality.None, NewIdeas = ActivityQuality.None,
            Tech = ActivityQuality.None, Travel = ActivityQuality.None,
            Astronomy = new()
            {
                Location = Clone(response.Location), ImportedAtUtc = DateTimeOffset.UtcNow, Source = Clone(source),
                Provenance = response.Provenance, Segments = Clone(source.LunarDaySegments), Events = Clone(source.Events)
            }
        };
        foreach (var s in day.Astronomy.Segments) { s.StartsAtUtc = Round(s.StartsAtUtc); s.EndsAtUtc = Round(s.EndsAtUtc); }
        foreach (var e in day.Astronomy.Events) e.AtUtc = Round(e.AtUtc);
        SyncLunarProjection(day);
        foreach (var p in source.Planets)
        {
            var transition = p.Transitions.OrderBy(t => t.AtUtc).LastOrDefault();
            var mapped = new PlanetInZodiac
            {
                Planet = (Planet)p.LegacyPlanetId, IsRetrograde = p.IsRetrograde,
                NewZodiacSign = (ZodiacSign)(transition?.ToLegacySignId ?? p.LegacyZodiacSignId),
                IsZodiacTransitioning = transition is not null,
                TransitionTime = transition is null ? default : TimeZoneInfo.ConvertTime(Round(transition.AtUtc), Vilnius).DateTime
            };
            if (transition is not null) mapped.PreviousZodiacSign = (ZodiacSign)transition.FromLegacySignId;
            Property(PlanetFields[p.LegacyPlanetId]).SetValue(day, mapped);
        }
        foreach (var e in source.Events.Where(e => e.Type == "aspect"))
        {
            var p1 = Array.IndexOf(BodyIds, e.BodyId); var p2 = Array.IndexOf(BodyIds, e.TargetBodyId);
            if (p1 < 0 || p2 < 0) throw new InvalidOperationException("Unknown body in aspect.");
            var aspect = e.AspectId switch { "conjunction" => AspectSymbol.Conjunction, "opposition" => AspectSymbol.Opposition, "trine" => AspectSymbol.Trine, "square" => AspectSymbol.Square, "sextile" => AspectSymbol.Sextile, _ => AspectSymbol.Other };
            day.PlanetEvents.Add(new() { Planet1 = (Planet)p1, Planet2 = (Planet)p2, AspectSymbol = aspect });
        }
        foreach (var group in Groups) day.Astronomy.Baseline[group] = Capture(day, group);
        return day;
    }

    public static void SyncLunarProjection(AstroEvent day)
    {
        var segments = day.Astronomy!.Segments;
        if (segments.Count == 0) return;
        day.MoonDay = CalendarDisplay.ProjectLunarDays(segments, Vilnius);
    }
}

public sealed record ImportPreview(AppDB Draft, List<ImportChange> Changes, List<CalculationNotice> Notices);
public sealed record ImportChange(DateOnly Date, bool Added, List<string> ChangedFields, List<string> PreservedFields);
