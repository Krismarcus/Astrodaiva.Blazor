using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astrodaiva.Blazor.Integration;

public sealed class CelestialCalendar
{
    public string SchemaVersion { get; set; } = "";
    public CalendarLocation Location { get; set; } = new();
    public List<CalculatedDay> AstronomyDays { get; set; } = new();
    public JsonElement? Provenance { get; set; }
    public List<CalculationNotice> Notices { get; set; } = new();
}

public sealed class CalendarLocation
{
    public string Label { get; set; } = "Vilnius, Lithuania";
    public string TimeZoneId { get; set; } = "Europe/Vilnius";
    public string ProviderPlaceId { get; set; } = "593116";
    public double LatitudeDeg { get; set; }
    public double LongitudeDeg { get; set; }
    public string Attribution { get; set; } = "";
}

public sealed class CalculationNotice
{
    public string Code { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class CalculatedDay
{
    public DateOnly Date { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public List<LunarSegment> LunarDaySegments { get; set; } = new();
    public CalculatedPhase MoonPhase { get; set; } = new();
    public bool SolarEclipse { get; set; }
    public bool LunarEclipse { get; set; }
    public List<CalculatedPlanet> Planets { get; set; } = new();
    public List<ExactEvent> Events { get; set; } = new();
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class LunarSegment
{
    public int LunarDayNumber { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public string StartBoundaryType { get; set; } = "";
    public string EndBoundaryType { get; set; } = "";
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class CalculatedPhase
{
    public string PhaseId { get; set; } = "";
    public int LegacyQuarterCode { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class CalculatedPlanet
{
    public string BodyId { get; set; } = "";
    public int LegacyPlanetId { get; set; }
    public int LegacyZodiacSignId { get; set; }
    public bool IsRetrograde { get; set; }
    public List<SignTransition> Transitions { get; set; } = new();
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class SignTransition
{
    public DateTimeOffset AtUtc { get; set; }
    public int FromLegacySignId { get; set; }
    public int ToLegacySignId { get; set; }
    public string Direction { get; set; } = "";
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class ExactEvent
{
    public string EventId { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTimeOffset AtUtc { get; set; }
    public string? BodyId { get; set; }
    public string? TargetBodyId { get; set; }
    public string? AspectId { get; set; }
    public string? PhaseId { get; set; }
    public string? Direction { get; set; }
    public int? LunarDayNumber { get; set; }
    public int? PreviousLunarDayNumber { get; set; }
    public string? Title { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

// Persisted with each calendar date. Baselines allow later imports to recognize edits.
public sealed class AstronomyMetadata
{
    public CalendarLocation Location { get; set; } = new();
    public DateTimeOffset ImportedAtUtc { get; set; }
    public CalculatedDay Source { get; set; } = new();
    public JsonElement? Provenance { get; set; }
    public List<LunarSegment> Segments { get; set; } = new();
    public List<ExactEvent> Events { get; set; } = new();
    public Dictionary<string, JsonElement> Baseline { get; set; } = new();
    public HashSet<string> Overrides { get; set; } = new();
    public bool IsBoundaryContext { get; set; }
}
