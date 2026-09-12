using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Astrodaiva.Api.Controllers;
using AstroDbContext = Astrodaiva.Api.Data.AstroDbContext;
using AppDbSnapshot = Astrodaiva.Api.Data.AppDbSnapshot;
using Astrodaiva.Api.Security;
using Astrodaiva.Blazor.Integration;
using Astrodaiva.Data.Enums;
using Astrodaiva.Data.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "september-2026.json");
var fixture = await File.ReadAllTextAsync(fixturePath);
var response = JsonSerializer.Deserialize<CelestialCalendar>(fixture, CalendarImporter.JsonOptions)!;
var count = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("FAILED: " + name); Console.WriteLine("PASS " + name); count++; }
AppDB Empty() => new() { AstroEventsDB = new(), MoonDayDetailsDB = new(), PlanetInZodiacsDB = new(), PlanetInRetrogradeDetailsDB = new() };
AstroEvent Day(AppDB db, int day) => db.AstroEventsDB.Single(e => e.Date == new DateTime(2026, 9, day));

var original = Empty();
original.MoonDayDetailsDB.Add(new() { MoonDay = 1, MoonDayInfo = "Keep LT", MoonDayInfoEn = "Keep EN" });
var imported = CalendarImporter.Preview(original, response, 2026, 9, false);
Check(original.AstroEventsDB.Count == 0, "preview leaves original untouched");
Check(imported.Draft.AstroEventsDB.Count == 30 && imported.Draft.AstronomyContext.Count == 4, "only active month becomes editable; four context dates retained");
var draft = imported.Draft;
Check(Day(draft, 11).MoonDay.PreviousMoonDay == 29 && Day(draft, 11).MoonDay.MiddleMoonDay == 1 && Day(draft, 11).MoonDay.NewMoonDay == 2, "real 29 → 1 → 2 sequence survives import");
Check(Day(draft, 6).Astronomy!.Segments.Count == 1, "single lunar segment remains single");
Check(Day(draft, 3).MoonInZodiac.NewZodiacSign == ZodiacSign.Gemini && Day(draft, 3).MoonInZodiac.PreviousZodiacSign == ZodiacSign.Taurus, "afternoon ingress uses destination sign rather than noon sign");
Check(Day(draft, 11).ImportantTasks == ActivityQuality.None, "new dates have unset activity ratings");
var again = CalendarImporter.Preview(CalendarImporter.Clone(draft), response, 2026, 9, false);
Check(again.Changes.All(c => !c.Added && c.ChangedFields.Count == 0 && c.PreservedFields.Count == 0), "identical reimport is idempotent after JSON roundtrip");
var edited = Day(draft, 11);
edited.EventText = "My LT title"; edited.EventTextEn = "My EN title"; edited.Barber = ActivityQuality.Good;
edited.MercuryInZodiac.NewZodiacSign = ZodiacSign.Cancer;
edited.Astronomy!.Segments[0].LunarDayNumber = 28;
CalendarImporter.SyncLunarProjection(edited);
CalendarImporter.DetectOverrides(edited);
var rerun = CalendarImporter.Preview(draft, response, 2026, 9, false).Draft;
Check(Day(rerun, 11).MercuryInZodiac.NewZodiacSign == ZodiacSign.Cancer && Day(rerun, 11).MoonDay.PreviousMoonDay == 28, "reimport preserves manual planet and lunar edits");
Check(Day(rerun, 11).EventText == "My LT title" && Day(rerun, 11).EventTextEn == "My EN title" && Day(rerun, 11).Barber == ActivityQuality.Good && rerun.MoonDayDetailsDB[0].MoonDayInfoEn == "Keep EN", "ratings, both languages, and interpretation library are preserved");
var reset = CalendarImporter.Preview(rerun, response, 2026, 9, true).Draft;
Check(Day(reset, 11).MercuryInZodiac.NewZodiacSign == ZodiacSign.Libra && Day(reset, 11).MoonDay.PreviousMoonDay == 29 && Day(reset, 11).EventText == "My LT title", "explicit reset restores astronomy only");
CalendarImporter.ValidateDraft(reset);
var la = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
var september10 = CalendarDisplay.EventsFor(reset, new(2026, 9, 10), la);
Check(september10.Any(e => e.Type == "moon-phase" && e.PhaseId == "new-moon"), "new moon moves from Sep 11 Vilnius to Sep 10 Los Angeles");
Check(!CalendarDisplay.EventsFor(reset, new(2026, 9, 11), la).Any(e => e.Type == "moon-phase" && e.PhaseId == "new-moon"), "event is not duplicated on its original date in another zone");
var bound = CalendarDisplay.Bounds(new(2026, 10, 25), CalendarImporter.Vilnius);
Check((bound.End - bound.Start).TotalHours == 25, "autumn DST day is 25 hours");
bound = CalendarDisplay.Bounds(new(2026, 3, 29), CalendarImporter.Vilnius);
Check((bound.End - bound.Start).TotalHours == 23, "spring DST day is 23 hours");
foreach (var zoneId in new[] { "Pacific/Kiritimati", "Pacific/Pago_Pago", "America/Los_Angeles", "Europe/Vilnius" })
{
    var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
    foreach (var date in new[] { new DateTime(2026, 9, 1), new DateTime(2026, 9, 30) })
    {
        var segments = CalendarDisplay.SegmentsFor(reset, date, zone); var range = CalendarDisplay.Bounds(date, zone);
        Check(segments.First().StartsAtUtc == range.Start && segments.Last().EndsAtUtc == range.End && segments.Zip(segments.Skip(1)).All(p => p.First.EndsAtUtc == p.Second.StartsAtUtc), $"full lunar coverage at month boundary {date:MM-dd} {zoneId}");
    }
}
var invalid = CalendarImporter.Clone(response); invalid.AstronomyDays.RemoveAt(0);
try { CalendarImporter.Preview(reset, invalid, 2026, 9, false); Check(false, "invalid import rejected"); } catch (InvalidOperationException) { Check(true, "incomplete import rejected atomically"); }
var afterEdit = Day(reset, 11); afterEdit.MercuryInZodiac.IsZodiacTransitioning = true; afterEdit.MercuryInZodiac.TransitionTime = new(2026, 9, 11, 15, 30, 0); CalendarImporter.DetectOverrides(afterEdit);
Check(CalendarDisplay.EventsFor(reset, new(2026, 9, 11), CalendarImporter.Vilnius).Any(e => e.EventId.StartsWith("manual-ingress") && e.AtUtc.UtcDateTime.Hour == 12), "manual transition is displayed at the edited instant");

// The real controllers and admin middleware run against an isolated SQLite database.
// The calculation transport is a captured response; no test contacts production.
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Configuration["CelestialMe:BaseUrl"] = "https://calculation.invalid/";
builder.Configuration["CelestialMe:ApiKey"] = "test-key-never-sent-to-network";
builder.Services.AddControllers().AddApplicationPart(typeof(ImportController).Assembly);
builder.Services.Configure<AdminAuthOptions>(o => { o.Password = "local-preview-only"; o.TokenSigningKey = "local-verification-signing-key-only-2026"; });
builder.Services.AddSingleton<AdminTokenService>();
var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
builder.Services.AddDbContext<AstroDbContext>(o => o.UseSqlite(connection));
builder.Services.AddSingleton<IHttpClientFactory>(new FixtureClientFactory(fixture));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:5088", "http://127.0.0.1:5088").AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("ETag", "Retry-After")));
var app = builder.Build();
app.UseCors(); app.UseMiddleware<AdminRequestMiddleware>(); app.MapControllers();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AstroDbContext>(); await db.Database.EnsureCreatedAsync();
    if (args.Contains("--serve"))
    {
        db.AppDbSnapshots.Add(new() { AppDbJson = JsonSerializer.Serialize(CalendarImporter.Preview(Empty(), response, 2026, 9, false).Draft), Label = "Local verification fixture", IsDefault = true }); await db.SaveChangesAsync();
    }
}
app.Urls.Add("http://127.0.0.1:5091"); await app.StartAsync();
if (args.Contains("--serve"))
{
    Console.WriteLine("Verification backend listening at http://127.0.0.1:5091. Test admin password: local-preview-only");
    await app.WaitForShutdownAsync(); return;
}
using var http = new HttpClient { BaseAddress = new("http://127.0.0.1:5091") };
Check((await http.GetAsync("api/import/snapshots")).StatusCode == HttpStatusCode.Unauthorized, "snapshot listing requires admin authentication");
Check((await http.PostAsJsonAsync("api/astronomy/month", new { year = 2026, month = 9 })).StatusCode == HttpStatusCode.Unauthorized, "calculation import requires admin authentication");
var login = await http.PostAsJsonAsync("api/auth/admin/login", new { password = "local-preview-only" });
var loginData = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData.RootElement.GetProperty("token").GetString());
var apiMonth = await http.PostAsJsonAsync("api/astronomy/month", new { year = 2026, month = 9 });
Check(apiMonth.IsSuccessStatusCode, "authenticated month import returns validated calculation data");
Check((await http.PostAsJsonAsync("api/astronomy/month", new { year = 2026, month = 13 })).StatusCode == HttpStatusCode.BadRequest, "invalid month is rejected");
var payload = JsonSerializer.Serialize(imported.Draft);
var savedDraft = await http.PostAsJsonAsync("api/import/full-sync", new { label = "Draft", setDefault = false, json = payload });
Check(savedDraft.IsSuccessStatusCode, "private backup can be saved before any publication");
Check((await http.GetAsync("api/import/default")).StatusCode == HttpStatusCode.NotFound, "first draft is never auto-published");
var publish = await http.PostAsJsonAsync("api/import/full-sync", new { label = "First publish", setDefault = true, json = payload, baseRevision = "none" });
Check(publish.IsSuccessStatusCode, "publish succeeds with expected revision");
var currentResponse = await http.GetAsync("api/import/default"); var revision = currentResponse.Headers.ETag!.Tag.Trim('"');
Check(revision != "none", "published calendar exposes revision ETag");
Check((await http.PostAsJsonAsync("api/import/full-sync", new { label = "Stale", setDefault = true, json = payload, baseRevision = "none" })).StatusCode == HttpStatusCode.Conflict, "stale draft cannot overwrite published calendar");
var before = await currentResponse.Content.ReadAsStringAsync();
var freshPayload = JsonSerializer.Deserialize<AppDB>(before)!; Day(freshPayload, 11).EventText = "Updated editorial";
var second = await http.PostAsJsonAsync("api/import/full-sync", new { label = "Second publish", setDefault = true, json = JsonSerializer.Serialize(freshPayload), baseRevision = revision });
Check(second.IsSuccessStatusCode, "subsequent edit publishes against current revision");
var nextRevision = (await http.GetAsync("api/import/default")).Headers.ETag!.Tag.Trim('"');
var firstPublishId = JsonDocument.Parse(await publish.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetInt64();
var rollback = await http.PostAsJsonAsync($"api/import/snapshots/{firstPublishId}/set-default", new { baseRevision = nextRevision });
Check(rollback.IsSuccessStatusCode && await http.GetStringAsync("api/import/default") == before, "rollback restores exact prior snapshot");
var withoutMetadata = CalendarImporter.Clone(freshPayload); foreach (var d in withoutMetadata.AstroEventsDB) d.Astronomy = null;
Check((await http.PostAsJsonAsync("api/import/full-sync", new { label = "Old browser", setDefault = true, json = JsonSerializer.Serialize(withoutMetadata), baseRevision = revision })).StatusCode == HttpStatusCode.Conflict, "older clients cannot discard astronomy metadata");
http.DefaultRequestHeaders.Authorization = null;
Check((await http.GetAsync($"api/import/snapshots/{firstPublishId}")).StatusCode == HttpStatusCode.Unauthorized, "individual backups remain private");
Check((await http.GetAsync("api/import/default")).IsSuccessStatusCode, "published calendar stays publicly readable");
await app.StopAsync(); await app.DisposeAsync(); await connection.DisposeAsync();
Console.WriteLine($"All {count} checks passed.");

sealed class FixtureClientFactory(string fixture) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(new FixtureHandler(fixture));
}
sealed class FixtureHandler(string fixture) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization?.Parameter != "test-key-never-sent-to-network") throw new Exception("Service key missing");
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        if (body.RootElement.GetProperty("startDate").GetString() != "2026-08-30" || body.RootElement.GetProperty("endDate").GetString() != "2026-10-02") throw new Exception("Incorrect padded month");
        return new(HttpStatusCode.OK) { Content = new StringContent(fixture, System.Text.Encoding.UTF8, "application/json") };
    }
}
