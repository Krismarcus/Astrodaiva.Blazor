using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Astrodaiva.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Astrodaiva.Api.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController(AstroDbContext db) : ControllerBase
{
    private static string Revision(AppDbSnapshot? snapshot) => snapshot is null
        ? "none" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot.AppDbJson)));

    private Task<AppDbSnapshot?> Current() => db.AppDbSnapshots.Where(x => x.IsDefault)
        .OrderByDescending(x => x.Id).FirstOrDefaultAsync();

    [HttpGet("default")]
    public async Task<IActionResult> GetDefaultSnapshot()
    {
        var snapshot = await Current();
        Response.Headers.ETag = $"\"{Revision(snapshot)}\"";
        Response.Headers.CacheControl = "no-cache";
        return snapshot is null ? NotFound() : Content(snapshot.AppDbJson, "application/json");
    }

    [HttpPost("full-sync")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> SaveSnapshot(SaveSnapshotRequest request)
    {
        try
        {
            using var payload = JsonDocument.Parse(request.Json);
            var events = payload.RootElement.GetProperty("AstroEventsDB");
            if (events.ValueKind != JsonValueKind.Array || events.GetArrayLength() == 0)
                return BadRequest(new { message = "The calendar must contain dates." });
            var dates = events.EnumerateArray().Select(e => DateTime.Parse(e.GetProperty("Date").GetString()!).Date).ToList();
            if (dates.Distinct().Count() != dates.Count)
                return BadRequest(new { message = "The calendar contains duplicate dates." });
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or FormatException or ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = "The calendar payload is invalid." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var current = await Current();
        if (request.SetDefault && request.BaseRevision != Revision(current))
            return Conflict(new { message = "The published calendar has changed. Download your draft, then reload the published calendar before publishing." });

        // Older browser versions must not silently remove imported astronomy metadata.
        if (request.SetDefault && current is not null)
        {
            using var oldDocument = JsonDocument.Parse(current.AppDbJson);
            using var newDocument = JsonDocument.Parse(request.Json);
            var incoming = newDocument.RootElement.GetProperty("AstroEventsDB").EnumerateArray()
                .ToDictionary(e => e.GetProperty("Date").GetString()!, e => e);
            foreach (var day in oldDocument.RootElement.GetProperty("AstroEventsDB").EnumerateArray())
            {
                if (day.TryGetProperty("Astronomy", out var metadata) && metadata.ValueKind == JsonValueKind.Object &&
                    (!incoming.TryGetValue(day.GetProperty("Date").GetString()!, out var next) ||
                     !next.TryGetProperty("Astronomy", out var nextMetadata) || nextMetadata.ValueKind != JsonValueKind.Object))
                    return Conflict(new { message = "This draft would remove imported astronomy. Reload using the updated app before publishing." });
            }
        }
        if (request.SetDefault)
            await db.AppDbSnapshots.Where(x => x.IsDefault).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
        var snapshot = new AppDbSnapshot
        {
            Label = string.IsNullOrWhiteSpace(request.Label) ? "Manual save" : request.Label.Trim()[..Math.Min(request.Label.Trim().Length, 200)],
            AppDbJson = request.Json,
            CreatedUtc = DateTime.UtcNow,
            IsDefault = request.SetDefault
        };
        db.AppDbSnapshots.Add(snapshot);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { snapshot.Id, snapshot.Label, snapshot.IsDefault, Revision = request.SetDefault ? Revision(snapshot) : Revision(current) });
    }

    [HttpGet("snapshots")]
    public async Task<IActionResult> ListSnapshots(int take = 50) => Ok(await db.AppDbSnapshots
        .OrderByDescending(x => x.Id).Take(Math.Clamp(take, 1, 200))
        .Select(x => new { x.Id, x.CreatedUtc, x.Label, x.IsDefault, SizeBytes = x.AppDbJson.Length }).ToListAsync());

    [HttpGet("snapshots/{id:long}")]
    public async Task<IActionResult> GetSnapshot(long id)
    {
        var snapshot = await db.AppDbSnapshots.FindAsync(id);
        return snapshot is null ? NotFound() : Content(snapshot.AppDbJson, "application/json");
    }

    [HttpPost("snapshots/{id:long}/set-default")]
    public async Task<IActionResult> SetDefault(long id, RestoreRequest request)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var current = await Current();
        if (request.BaseRevision != Revision(current))
            return Conflict(new { message = "The published calendar changed. Reload before restoring a backup." });
        var snapshot = await db.AppDbSnapshots.SingleOrDefaultAsync(x => x.Id == id);
        if (snapshot is null) return NotFound();
        await db.AppDbSnapshots.Where(x => x.IsDefault).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
        // ExecuteUpdate bypasses EF tracking; explicitly mark this property as modified.
        snapshot.IsDefault = true;
        db.Entry(snapshot).Property(x => x.IsDefault).IsModified = true;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { id, isDefault = true, Revision = Revision(snapshot) });
    }

    [HttpDelete("snapshots/{id:long}")]
    public async Task<IActionResult> DeleteSnapshot(long id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var snapshot = await db.AppDbSnapshots.SingleOrDefaultAsync(x => x.Id == id);
        if (snapshot is null) return NotFound();
        if (snapshot.IsDefault) return Conflict(new { message = "Restore another backup before deleting the published calendar." });
        db.AppDbSnapshots.Remove(snapshot);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { deleted = true, id });
    }

    public record SaveSnapshotRequest(string? Label, bool SetDefault, string Json, string? BaseRevision);
    public record RestoreRequest(string? BaseRevision);
}
