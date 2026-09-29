using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Astrodaiva.Api.Controllers;
using Astrodaiva.Blazor.Integration;
using Astrodaiva.Data.Enums;
using Astrodaiva.Data.Models;
using Microsoft.AspNetCore.Mvc;

static class DiamondMigrationChecks
{
    public static void Run(Action<bool, string> check, CelestialCalendar response, AppDB legacyImport)
    {
        var original = CalendarImporter.Clone(legacyImport);
        original.ShowExactEvents = true;
        original.ShowAspectSymbols = true;
        original.HiddenYears.Add(2026);
        var edited = original.AstroEventsDB.Single(day => day.Date == new DateTime(2026, 9, 11));
        edited.EventText = "Manual LT"; edited.EventTextEn = "Manual EN";
        edited.EventDescription = "Description LT"; edited.EventDescriptionEn = "Description EN";
        edited.HideEventText = true;
        edited.MercuryInZodiac.NewZodiacSign = ZodiacSign.Cancer;
        edited.Astronomy!.Segments[0].LunarDayNumber = 28;
        CalendarImporter.SyncLunarProjection(edited);
        CalendarImporter.DetectOverrides(edited);
        var before = JsonSerializer.Serialize(original);
        var preview = CalendarImporter.Preview(original, response, 2026, 9, false);
        var draft = preview.Draft;
        var day = draft.AstroEventsDB.Single(day => day.Date == edited.Date);
        check(JsonSerializer.Serialize(original) == before, "new provider preview does not mutate the saved calendar");
        check(day.MercuryInZodiac.NewZodiacSign == ZodiacSign.Cancer && day.MoonDay.PreviousMoonDay == 28,
            "switching providers preserves earlier manual planet and lunar overrides");
        foreach (var reset in new[] { false, true })
        {
            var next = reset ? CalendarImporter.Preview(original, response, 2026, 9, true).Draft : draft;
            CalendarImporter.ValidateDraft(next);
            check(original.AstroEventsDB.Count == next.AstroEventsDB.Count && original.AstroEventsDB.Zip(next.AstroEventsDB).All(pair =>
                pair.First.Date == pair.Second.Date && Editorial(pair.First) == Editorial(pair.Second)),
                $"new provider import preserves every yearly activity rating and authored field (reset={reset})");
            check(next.ShowExactEvents && next.ShowAspectSymbols && next.HiddenYears.SequenceEqual(new[] { 2026 }),
                $"new provider import preserves display and year visibility settings (reset={reset})");
        }
        check(day.Astronomy!.Provenance!.Value.GetProperty("bindingVersion").GetString()!.StartsWith("Astronomy Engine") &&
              day.Astronomy.Source.Extra!.ContainsKey("lunarDayTransitions"),
            "new provider provenance and additional source fields survive import");
        var resetDay = CalendarImporter.Preview(original, response, 2026, 9, true).Draft.AstroEventsDB.Single(d => d.Date == edited.Date);
        check(resetDay.MoonDay.PreviousMoonDay == 29 && resetDay.MoonDay.MiddleMoonDay == 1 && resetDay.MoonDay.NewMoonDay == 2 &&
              resetDay.MoonDay.MiddleMoonDayTransitionTime == new DateTime(2026, 9, 11, 6, 27, 28) &&
              resetDay.MoonDay.TransitionTime == new DateTime(2026, 9, 11, 6, 58, 40),
            "Astronomy Engine triple lunar day retains the original timeline with its new exact transition times");
        check(CalendarImporter.Preview(draft, response, 2026, 9, false).Changes.All(change => !change.Added && change.ChangedFields.Count == 0),
            "reimport from the new provider is idempotent and keeps manual overrides");
    }

    private static string Editorial(AstroEvent day) => JsonSerializer.Serialize(new
    {
        day.ImportantTasks, day.Contracts, day.Meetings, day.Love, day.Buystuff, day.Beauty,
        day.Barber, day.Gardening, day.NewIdeas, day.Tech, day.Travel,
        day.EventText, day.EventTextEn, day.EventDescription, day.EventDescriptionEn, day.HideEventText
    });

    public static async Task CheckProxyErrors(Action<bool, string> check)
    {
        foreach (var status in new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.TooManyRequests, HttpStatusCode.ServiceUnavailable })
        {
            var factory = new FailureFactory(status);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CelestialMe:ApiKey"] = "fake-migration-test-key"
            }).Build();
            var controller = new AstronomyController(factory, configuration)
            {
                ControllerContext = new() { HttpContext = new DefaultHttpContext() }
            };
            var result = await controller.Month(new(2026, 9), CancellationToken.None) as ObjectResult;
            check(result?.StatusCode == (status == HttpStatusCode.TooManyRequests ? 429 : 502) && factory.Calls == 1 &&
                  !JsonSerializer.Serialize(result.Value).Contains("private-provider-body") &&
                  controller.Response.Headers["X-Upstream-Request-Id"] == "migration-test",
                $"provider {(int)status} is handled without retrying or exposing its body; support request ID is retained");
            if (status == HttpStatusCode.TooManyRequests)
                check(controller.Response.Headers.RetryAfter == "60", "provider rate limit preserves Retry-After");
        }
    }

    private sealed class FailureFactory(HttpStatusCode status) : HttpMessageHandler, IHttpClientFactory
    {
        public int Calls { get; private set; }
        public HttpClient CreateClient(string name) => new(this, disposeHandler: false);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var response = new HttpResponseMessage(status) { Content = new StringContent("private-provider-body") };
            response.Headers.Add("X-Request-Id", "migration-test");
            if (status == HttpStatusCode.TooManyRequests) response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
            return Task.FromResult(response);
        }
    }
}
