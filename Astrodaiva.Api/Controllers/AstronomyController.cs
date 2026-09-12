using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Astrodaiva.Api.Controllers;

// All routes are protected by the admin middleware, including GET diagnostics.
[ApiController]
[Route("api/astronomy")]
public sealed class AstronomyController(IHttpClientFactory clients, IConfiguration configuration) : ControllerBase
{
    [HttpPost("month")]
    public async Task<IActionResult> Month(MonthRequest request, CancellationToken cancellationToken)
    {
        if (request.Year is < 1900 or > 2100 || request.Month is < 1 or > 12)
            return BadRequest(new { message = "Choose a month between 1900 and 2100." });

        var key = configuration["CelestialMe:ApiKey"];
        var baseUrl = configuration["CelestialMe:BaseUrl"];
        if (string.IsNullOrWhiteSpace(key) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            return StatusCode(503, new { message = "Calendar import is not configured. Set CelestialMe:BaseUrl and CelestialMe:ApiKey on the backend." });

        var first = new DateOnly(request.Year, request.Month, 1);
        // Two civil dates on either side cover the largest possible difference between zones.
        var start = first.AddDays(-2);
        var end = first.AddMonths(1).AddDays(1);
        using var client = clients.CreateClient("CelestialMe");
        var endpoint = new Uri(new Uri(baseUrl!.TrimEnd('/') + "/"), "v1/consumer/integrations/astrodaiva/calendar");
        try
        {
            using var upstream = new HttpRequestMessage(HttpMethod.Post, endpoint);
            upstream.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            upstream.Content = JsonContent.Create(new
            {
                location = new { providerPlaceId = configuration["CelestialMe:ProviderPlaceId"] ?? "593116" },
                startDate = start.ToString("yyyy-MM-dd"), endDate = end.ToString("yyyy-MM-dd")
            });
            using var response = await client.SendAsync(upstream, cancellationToken);
            if (response.Headers.TryGetValues("X-Request-Id", out var ids))
                Response.Headers["X-Upstream-Request-Id"] = ids.FirstOrDefault();
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests && response.Headers.RetryAfter is { } retry)
                    Response.Headers.RetryAfter = retry.ToString();
                // Do not expose provider bodies or authentication details, and do not automatically retry calculations.
                var message = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "The calculation service key is invalid or expired. Update the backend key.",
                    HttpStatusCode.Forbidden => "The calculation service key does not permit calendar imports.",
                    HttpStatusCode.TooManyRequests => "The calculation service is busy. Wait before importing again.",
                    _ => "The calculation service could not complete this import. No calendar data was changed."
                };
                return StatusCode(response.StatusCode == HttpStatusCode.TooManyRequests ? 429 : 502, new { message });
            }
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var days = root.GetProperty("astronomyDays");
            if (root.GetProperty("schemaVersion").GetString() != "1.0" ||
                root.GetProperty("range").GetProperty("timeZoneId").GetString() != "Europe/Vilnius" ||
                days.GetArrayLength() != end.DayNumber - start.DayNumber + 1)
                throw new JsonException("Unexpected calendar contract.");
            var dates = days.EnumerateArray().Select(d => DateOnly.Parse(d.GetProperty("date").GetString()!)).Order().ToArray();
            if (!dates.SequenceEqual(Enumerable.Range(0, dates.Length).Select(i => start.AddDays(i))))
                throw new JsonException("Missing or duplicate dates.");
            return Content(json, "application/json");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new { message = "The calculation service timed out. No calendar data was changed." });
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or FormatException or InvalidOperationException)
        {
            return StatusCode(502, new { message = "The calculation service returned an unavailable or incompatible calendar. No calendar data was changed." });
        }
    }

    public record MonthRequest(int Year, int Month);
}
