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
Check(!draft.ShowExactEvents, "exact events are hidden by default for existing calendars");
var visibleEvents = CalendarImporter.Clone(draft); visibleEvents.ShowExactEvents = true;
Check(CalendarImporter.Preview(CalendarImporter.Clone(visibleEvents), response, 2026, 9, false).Draft.ShowExactEvents, "display preference survives snapshot roundtrip and month reimport");
Check(Day(draft, 11).MoonDay.PreviousMoonDay == 29 && Day(draft, 11).MoonDay.MiddleMoonDay == 1 && Day(draft, 11).MoonDay.NewMoonDay == 2, "real 29 → 1 → 2 sequence survives import");
Check(Day(draft, 6).Astronomy!.Segments.Count == 1, "single lunar segment remains single");
Check(Day(draft, 3).MoonInZodiac.NewZodiacSign == ZodiacSign.Gemini && Day(draft, 3).MoonInZodiac.PreviousZodiacSign == ZodiacSign.Taurus, "afternoon ingress uses destination sign rather than noon sign");
Check(Day(draft, 11).ImportantTasks == ActivityQuality.None, "new dates have unset activity ratings");
var again = CalendarImporter.Preview(CalendarImporter.Clone(draft), response, 2026, 9, false);
Check(again.Changes.All(c => !c.Added && c.ChangedFields.Count == 0 && c.PreservedFields.Count == 0), "identical reimport is idempotent after JSON roundtrip");
var edited = Day(draft, 11);
edited.EventText = "My LT title"; edited.EventTextEn = "My EN title"; edited.Barber = ActivityQuality.Good;
edited.EventDescription = "My LT description"; edited.EventDescriptionEn = "My EN description";
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
var aspectDraft = CalendarImporter.Clone(reset);
Check(!aspectDraft.ShowAspectSymbols, "aspect symbols are off by default for existing snapshots");
aspectDraft.ShowAspectSymbols = true;
Day(aspectDraft, 11).HideEventText = true;
foreach (var resetAspects in new[] { false, true })
{
    var reimportedAspects = CalendarImporter.Preview(CalendarImporter.Clone(aspectDraft), response, 2026, 9, resetAspects).Draft;
    var authored = Day(reimportedAspects, 11);
    Check(reimportedAspects.ShowAspectSymbols && authored.HideEventText && authored.EventText == "My LT title" && authored.EventTextEn == "My EN title" && authored.EventDescription == "My LT description" && authored.EventDescriptionEn == "My EN description", $"import preserves hidden event text, both languages, and aspect preference with reset={resetAspects}");
}
var hiddenTextPublicCopy = CalendarVisibility.PublicCopy(aspectDraft);
Check(Day(hiddenTextPublicCopy, 11).HideEventText && Day(hiddenTextPublicCopy, 11).EventTextEn == "My EN title" && !Day(imported.Draft, 12).HideEventText, "No Text visibility survives publication without deleting text or hiding other dates");
var aspectDay = aspectDraft.AstroEventsDB.First(d => d.Astronomy!.Events.Any(e => e.Type == "aspect"));
var originalAspects = AspectDisplay.ForDate(aspectDraft, aspectDay.Date);
Check(originalAspects.Count > 0 && originalAspects.Count == aspectDay.PlanetEvents.Select(e => (e.Planet1, e.Planet2, e.AspectSymbol)).Distinct().Count(), "imported aspects appear once instead of duplicating their legacy projection");
aspectDay.Astronomy!.Events.RemoveAll(e => e.Type == "aspect");
Check(AspectDisplay.ForDate(aspectDraft, aspectDay.Date).Count == 0 && aspectDay.PlanetEvents.Count > 0, "deleted imported aspects cannot reappear from stale legacy data");
var legacyAspects = Empty();
legacyAspects.AstroEventsDB.Add(new() { Date = new(2026, 9, 12), PlanetEvents = new() {
    new() { Planet1 = Planet.Sun, Planet2 = Planet.Moon, AspectSymbol = AspectSymbol.Square },
    new() { Planet1 = Planet.Venus, Planet2 = Planet.Mars, AspectSymbol = AspectSymbol.Trine },
    new() { Planet1 = Planet.Sun, Planet2 = Planet.Moon, AspectSymbol = AspectSymbol.Other }
} });
Check(AspectDisplay.ForDate(legacyAspects, new(2026, 9, 12)).Select(e => AspectDisplay.Symbol(e.AspectSymbol)).SequenceEqual(new[] { "□", "△" }) && AspectDisplay.ForDate(legacyAspects, new(2026, 9, 13)).Count == 0, "legacy aspects use symbols for the selected date and omit unsupported relationships");
var la = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
var timeline = CalendarDisplay.LunarTimelineFor(reset, new(2026, 9, 10), la)!;
Check(timeline.PreviousMoonDay == 29 && timeline.MiddleMoonDay == 1 && timeline.NewMoonDay == 2 && timeline.IsTripleMoonDay,
    "visitor timeline assigns the lunar day contained within the date to the middle");
Check(timeline.MiddleMoonDayTransitionTime == new DateTime(2026, 9, 10, 20, 27, 0) && timeline.TransitionTime == new DateTime(2026, 9, 10, 20, 58, 43),
    "timeline contains only actual transition starts in the visitor time zone");
var singleDay = CalendarDisplay.LunarTimelineFor(reset, new(2026, 9, 6), CalendarImporter.Vilnius)!;
Check(singleDay.PreviousMoonDay == singleDay.NewMoonDay && singleDay.MiddleMoonDay == 0 && singleDay.TransitionTime == default,
    "unchanged lunar day has no synthetic midnight transition");
var legacyOnly = Empty(); legacyOnly.AstroEventsDB.Add(new() { Date = new(2026, 9, 10), MoonDay = new() { NewMoonDay = 29, PreviousMoonDay = 28, TransitionTime = new(2026, 9, 10, 8, 15, 0) } });
Check(ReferenceEquals(CalendarDisplay.LunarTimelineFor(legacyOnly, new(2026, 9, 10), la), legacyOnly.AstroEventsDB[0].MoonDay),
    "manual calendar dates retain their original timeline data");
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
var yearly = Empty();
var ratingFields = new[] { "ImportantTasks", "Contracts", "Meetings", "Love", "Buystuff", "Beauty", "Barber", "Gardening", "NewIdeas", "Tech", "Travel" };
for (var i = 0; i < 365; i++)
{
    var day = new AstroEvent { Date = new DateTime(2026, 1, 1).AddDays(i) };
    for (var f = 0; f < ratingFields.Length; f++)
        typeof(AstroEvent).GetProperty(ratingFields[f])!.SetValue(day, (i + f) % 2 == 0 ? ActivityQuality.Good : ActivityQuality.Bad);
    yearly.AstroEventsDB.Add(day);
}
var yearlyImport = CalendarImporter.Preview(CalendarImporter.Clone(yearly), response, 2026, 9, false).Draft;
Check(yearlyImport.AstroEventsDB.Count == 365 && yearly.AstroEventsDB.Zip(yearlyImport.AstroEventsDB).All(pair =>
    pair.First.Date == pair.Second.Date && ratingFields.All(field => Equals(typeof(AstroEvent).GetProperty(field)!.GetValue(pair.First), typeof(AstroEvent).GetProperty(field)!.GetValue(pair.Second)))),
    "month import preserves every saved Good/Bad activity rating across the full year");
var vilniusTimeline = CalendarDisplay.LunarTimelineFor(reset, new(2026, 9, 11), CalendarImporter.Vilnius)!;
Check(vilniusTimeline.MiddleMoonDayTransitionTime == new DateTime(2026, 9, 11, 6, 27, 0) && vilniusTimeline.TransitionTime == new DateTime(2026, 9, 11, 6, 58, 43),
    "shared calendar uses original Vilnius transition times");
var visibilityDraft = CalendarImporter.Clone(yearlyImport);
visibilityDraft.AstroEventsDB.Add(new AstroEvent { Date = new(2028, 2, 29), Barber = ActivityQuality.Bad });
visibilityDraft.HiddenYears.Add(2026);
var publicCopy = CalendarVisibility.PublicCopy(visibilityDraft);
Check(CalendarVisibility.Years(publicCopy).SequenceEqual(new[] { 2028 }) && CalendarVisibility.Years(visibilityDraft, true).SequenceEqual(new[] { 2026, 2028 }), "hidden years remain available in draft preview only");
Check(visibilityDraft.AstroEventsDB.Count == 366 && publicCopy.AstroEventsDB.Count == 1 && publicCopy.AstronomyContext.Count == 0, "visibility filtering preserves draft dates and removes hidden context");
publicCopy.AstroEventsDB[0].Barber = ActivityQuality.Good;
Check(visibilityDraft.AstroEventsDB.Last().Barber == ActivityQuality.Bad, "public and draft calendar data do not share mutable ratings");
Check(CalendarVisibility.Move(new(2026, 12, 31), 1, new[] { 2026, 2028 }, true) == new DateTime(2028, 1, 1) && CalendarVisibility.Move(new(2028, 1, 1), -1, new[] { 2026, 2028 }, false) == new DateTime(2026, 12, 31), "month and day navigation skip unavailable years in both directions");
Check(CalendarVisibility.Move(new(2026, 1, 1), -1, new[] { 2026 }, true) is null && CalendarVisibility.Move(new(2026, 12, 31), 1, new[] { 2026 }, false) is null && CalendarVisibility.Move(new(2026, 9, 1), 1, Array.Empty<int>(), true) is null, "calendar navigation stops at available boundaries including an empty calendar");
Check(CalendarVisibility.Nearest(new(2028, 2, 29), new[] { 2026 }) == new DateTime(2026, 2, 28), "opening a hidden leap year chooses a valid visible date");
Check(CalendarImporter.Preview(visibilityDraft, response, 2026, 9, false).Draft.HiddenYears.SequenceEqual(new[] { 2026 }), "import preserves year visibility settings");
if (args.Contains("--calculations-only"))
{
    Console.WriteLine($"All {count} calendar checks passed.");
    return;
}
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
        var calendarArgument = Array.IndexOf(args, "--calendar");
        if (calendarArgument < 0 || calendarArgument + 1 >= args.Length)
            throw new InvalidOperationException("Local previews require --calendar <published-calendar.json> so saved activity ratings are retained.");
        var previewJson = await File.ReadAllTextAsync(args[calendarArgument + 1]);
        var previewCalendar = JsonSerializer.Deserialize<AppDB>(previewJson, CalendarImporter.JsonOptions);
        if (previewCalendar?.AstroEventsDB is not { Count: > 0 }) throw new InvalidOperationException("The preview calendar contains no saved dates.");
        db.AppDbSnapshots.Add(new() { AppDbJson = previewJson, Label = "Local copy of published calendar", IsDefault = true }); await db.SaveChangesAsync();
    }
}
app.Urls.Add(args.Contains("--serve") ? "http://127.0.0.1:5091" : "http://127.0.0.1:0"); await app.StartAsync();
if (args.Contains("--serve"))
{
    Console.WriteLine("Verification backend listening at http://127.0.0.1:5091. Test admin password: local-preview-only");
    await app.WaitForShutdownAsync(); return;
}
using var http = new HttpClient { BaseAddress = new(app.Urls.Single()) };
Check((await http.GetAsync("api/import/admin-default")).StatusCode == HttpStatusCode.Unauthorized, "full calendar including hidden years requires admin authentication");
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

// Exercise public filtering and real client-store isolation against the same controllers.
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData.RootElement.GetProperty("token").GetString());
var hiddenPayload = CalendarImporter.Clone(freshPayload);
hiddenPayload.AstroEventsDB.Add(new AstroEvent { Date = new(2025, 12, 31), Barber = ActivityQuality.Bad, EventText = "Private legacy year" });
hiddenPayload.AstroEventsDB.Add(new AstroEvent { Date = new(2027, 1, 1), Barber = ActivityQuality.Good });
hiddenPayload.HiddenYears = new() { 2025, 2026 };
var hiddenPublish = await http.PostAsJsonAsync("api/import/full-sync", new { label = "Hidden years", setDefault = true, json = JsonSerializer.Serialize(hiddenPayload), baseRevision = revision });
Check(hiddenPublish.IsSuccessStatusCode, "year visibility publishes with the calendar revision");
var filteredResponse = await http.GetAsync("api/import/default");
var filteredCalendar = JsonSerializer.Deserialize<AppDB>(await filteredResponse.Content.ReadAsStringAsync())!;
Check(filteredCalendar.AstroEventsDB.Count == 1 && filteredCalendar.AstroEventsDB[0].Date.Year == 2027 && filteredCalendar.AstronomyContext.Count == 0, "public endpoint removes hidden years and their astronomy context");
var adminResponse = await http.GetAsync("api/import/admin-default");
var adminCalendar = JsonSerializer.Deserialize<AppDB>(await adminResponse.Content.ReadAsStringAsync())!;
Check(adminCalendar.AstroEventsDB.Count == 32 && adminCalendar.AstroEventsDB.Single(d => d.Date.Year == 2025).Barber == ActivityQuality.Bad && adminResponse.Headers.CacheControl!.NoStore && adminResponse.Headers.ETag!.Tag == filteredResponse.Headers.ETag!.Tag, "admin receives complete saved data without caching and the same publication revision");
var incompletePrivate = CalendarImporter.Clone(hiddenPayload);
incompletePrivate.AstroEventsDB.Remove(incompletePrivate.AstroEventsDB.Single(d => d.Date.Year == 2025));
Check((await http.PostAsJsonAsync("api/import/full-sync", new { label = "Incomplete calendar", setDefault = true, json = JsonSerializer.Serialize(incompletePrivate), baseRevision = adminResponse.Headers.ETag!.Tag.Trim('"') })).StatusCode == HttpStatusCode.Conflict, "publishing cannot silently discard hidden legacy dates");
var invalidYears = JsonSerializer.Serialize(hiddenPayload).Replace("[2025,2026]", "[0,10000]");
Check((await http.PostAsJsonAsync("api/import/full-sync", new { label = "Invalid years", setDefault = false, json = invalidYears })).StatusCode == HttpStatusCode.BadRequest, "invalid visibility years are rejected");
var storeApi = new Astrodaiva.Blazor.Services.AstroApiClient(http);
storeApi.SetAdminToken(loginData.RootElement.GetProperty("token").GetString());
using var localLibrary = new HttpClient(new LibraryHandler(JsonSerializer.Serialize(Empty()))) { BaseAddress = new("https://local.invalid/") };
var store = new Astrodaiva.Blazor.Services.AstroDbStore(localLibrary, storeApi);
await store.EnsurePublicLoadedAsync(); await store.BeginEditingAsync();
store.Db!.HiddenYears.Clear();
store.Db.AstroEventsDB.Single(d => d.Date.Year == 2027).Barber = ActivityQuality.Bad;
store.DraftChangeCount = 2;
Check(store.PublishedDb!.AstroEventsDB.Count == 1 && store.PublishedDb.AstroEventsDB[0].Barber == ActivityQuality.Good, "admin visibility and rating edits do not leak into public views");
await store.RetryDefaultSnapshotAsync(); await store.BeginEditingAsync();
Check(store.Db.HiddenYears.Count == 0 && store.DraftChangeCount == 2, "public refresh and returning to admin preserve the current draft");
await store.SaveSnapshotAsync("Private edited draft", false);
Check((await http.GetFromJsonAsync<AppDB>("api/import/default"))!.AstroEventsDB.Count == 1, "saving a private draft does not change live year visibility");
await store.SaveSnapshotAsync("Show all years", true);
Check(store.PublishedDb!.AstroEventsDB.Count == 32 && (await http.GetFromJsonAsync<AppDB>("api/import/default"))!.AstroEventsDB.Count == 32, "publishing makes selected years visible in both API and client");
store.Db.HiddenYears = new() { 2025, 2026, 2027 };
await store.SaveSnapshotAsync("Hide all years", true);
Check(store.PublishedDb!.AstroEventsDB.Count == 0 && store.Db.AstroEventsDB.Count == 32 && (await http.GetFromJsonAsync<AppDB>("api/import/default"))!.AstroEventsDB.Count == 0, "all years can be hidden while their full data remains editable");
var draftRevision = storeApi.PublishedRevision;
await http.PostAsJsonAsync($"api/import/snapshots/{firstPublishId}/set-default", new { baseRevision = draftRevision });
await store.RetryDefaultSnapshotAsync();
Check(storeApi.PublishedRevision == draftRevision && store.Db.HiddenYears.Count == 3 && store.PublishedDb!.AstroEventsDB.Count == 30, "a live refresh cannot silently rebase a stale admin draft");
try { await store.SaveSnapshotAsync("Stale admin publish", true); Check(false, "stale client draft rejected"); }
catch (InvalidOperationException) { Check(true, "client rejects stale publication after another admin changes the live calendar"); }
await app.StopAsync(); await app.DisposeAsync(); await connection.DisposeAsync();
Console.WriteLine($"All {count} checks passed.");

sealed class LibraryHandler(string json) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
}
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
