using Astrodaiva.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Astrodaiva.Api.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController : ControllerBase
{
    private readonly AstroDbContext _db;

    public ImportController(AstroDbContext db) => _db = db;

    // POST /api/import/full-sync
    // Saves the entire AppDB payload as a snapshot only.
    // Rule:
    // - if first snapshot -> always default
    // - if req.SetDefault -> set as default (and unset others)
    // - otherwise keep existing default (if any)
    [HttpPost("full-sync")]
    public async Task<IActionResult> SaveSnapshot([FromBody] SaveSnapshotRequest req)
    {
        var total = await _db.AppDbSnapshots.CountAsync();

        var makeDefault = total == 0 || req.SetDefault;

        if (makeDefault)
        {
            // unset all defaults
            await _db.AppDbSnapshots.ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.IsDefault, false));
        }

        var snap = new AppDbSnapshot
        {
            Label = string.IsNullOrWhiteSpace(req.Label) ? "ManualSave" : req.Label.Trim(),
            AppDbJson = req.Json,              // <-- keep your property name
            CreatedUtc = DateTime.UtcNow,      // <-- keep your property name
            IsDefault = makeDefault
        };

        _db.AppDbSnapshots.Add(snap);
        await _db.SaveChangesAsync();

        // If this is now the only snapshot, force default (safety)
        // (not really needed here, but keeps rule consistent)
        var countAfter = await _db.AppDbSnapshots.CountAsync();
        if (countAfter == 1 && !snap.IsDefault)
        {
            snap.IsDefault = true;
            await _db.SaveChangesAsync();
        }

        return Ok(new { snap.Id, snap.Label, snap.IsDefault });
    }

    // GET /api/import/default
    // Returns the JSON of the default snapshot (if any).
    // Rule:
    // - if only one snapshot exists, treat it as default even if DB flag is wrong
    [HttpGet("default")]
    public async Task<IActionResult> GetDefaultSnapshot()
    {
        var total = await _db.AppDbSnapshots.CountAsync();

        if (total == 0)
            return NotFound();

        AppDbSnapshot? snap;

        if (total == 1)
        {
            // Only one snapshot -> always default
            snap = await _db.AppDbSnapshots
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }
        else
        {
            // Many snapshots -> use explicit default
            snap = await _db.AppDbSnapshots
                .Where(x => x.IsDefault)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            // Safety: if none marked default, pick newest and make it default
            if (snap is null)
            {
                snap = await _db.AppDbSnapshots
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (snap is not null)
                {
                    await _db.AppDbSnapshots.ExecuteUpdateAsync(s =>
                        s.SetProperty(x => x.IsDefault, false));

                    snap.IsDefault = true;
                    await _db.SaveChangesAsync();
                }
            }
        }

        if (snap is null) return NotFound();
        return Content(snap.AppDbJson, "application/json");
    }

    // GET /api/import/snapshots?take=50
    // Rule:
    // - if only one snapshot, return IsDefault=true in response
    // - if multiple, ensure only one is default (self-heal if needed)
    [HttpGet("snapshots")]
    public async Task<IActionResult> ListSnapshots([FromQuery] int take = 50)
    {
        take = Math.Clamp(take, 1, 200);

        var total = await _db.AppDbSnapshots.CountAsync();

        // Self-heal default state if needed when multiple exist
        if (total > 1)
        {
            var defaultCount = await _db.AppDbSnapshots.CountAsync(x => x.IsDefault);
            if (defaultCount == 0)
            {
                var newest = await _db.AppDbSnapshots
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (newest is not null)
                {
                    newest.IsDefault = true;
                    await _db.SaveChangesAsync();
                }
            }
            else if (defaultCount > 1)
            {
                // keep newest default, unset others
                var newestDefaultId = await _db.AppDbSnapshots
                    .Where(x => x.IsDefault)
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.Id)
                    .FirstAsync();

                await _db.AppDbSnapshots
                    .Where(x => x.IsDefault && x.Id != newestDefaultId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
            }
        }
        else if (total == 1)
        {
            // Ensure DB flag is true for the only snapshot (optional, but consistent)
            var only = await _db.AppDbSnapshots.FirstOrDefaultAsync();
            if (only is not null && !only.IsDefault)
            {
                only.IsDefault = true;
                await _db.SaveChangesAsync();
            }
        }

        var items = await _db.AppDbSnapshots
            .OrderByDescending(x => x.Id)
            .Take(take)
            .Select(x => new
            {
                x.Id,
                x.CreatedUtc,
                x.Label,
                x.IsDefault,
                SizeBytes = x.AppDbJson.Length
            })
            .ToListAsync();

        // If only one snapshot, always show as default in response
        if (items.Count == 1)
        {
            items[0] = new
            {
                items[0].Id,
                items[0].CreatedUtc,
                items[0].Label,
                IsDefault = true,
                items[0].SizeBytes
            };
        }

        return Ok(items);
    }

    // POST /api/import/snapshots/{id}/set-default
    // Rule:
    // - if only one snapshot exists, just force it default
    // - if multiple, unset all and set this one
    [HttpPost("snapshots/{id:long}/set-default")]
    public async Task<IActionResult> SetDefault([FromRoute] long id)
    {
        var total = await _db.AppDbSnapshots.CountAsync();
        if (total == 0) return NotFound();

        var snap = await _db.AppDbSnapshots.SingleOrDefaultAsync(x => x.Id == id);
        if (snap is null) return NotFound();

        if (total == 1)
        {
            if (!snap.IsDefault)
            {
                snap.IsDefault = true;
                await _db.SaveChangesAsync();
            }
            return Ok(new { id, isDefault = true });
        }

        await _db.AppDbSnapshots.ExecuteUpdateAsync(s =>
            s.SetProperty(x => x.IsDefault, false));

        snap.IsDefault = true;
        await _db.SaveChangesAsync();

        return Ok(new { id, isDefault = true });
    }

    // GET /api/import/snapshots/{id}
    [HttpGet("snapshots/{id:long}")]
    public async Task<IActionResult> GetSnapshot([FromRoute] long id)
    {
        var snap = await _db.AppDbSnapshots.SingleOrDefaultAsync(x => x.Id == id);
        if (snap is null) return NotFound();
        return Content(snap.AppDbJson, "application/json");
    }

    // DELETE /api/import/snapshots/{id}
    // Rule:
    // - If deleted snapshot was default and others remain -> promote newest to default
    [HttpDelete("snapshots/{id:long}")]
    public async Task<IActionResult> DeleteSnapshot([FromRoute] long id)
    {
        var deleted = await _db.AppDbSnapshots.SingleOrDefaultAsync(x => x.Id == id);
        if (deleted is null) return NotFound();

        var deletedWasDefault = deleted.IsDefault;

        _db.AppDbSnapshots.Remove(deleted);
        await _db.SaveChangesAsync();

        // After delete: enforce rules
        var total = await _db.AppDbSnapshots.CountAsync();

        if (total == 1)
        {
            var only = await _db.AppDbSnapshots.FirstOrDefaultAsync();
            if (only is not null && !only.IsDefault)
            {
                only.IsDefault = true;
                await _db.SaveChangesAsync();
            }
        }
        else if (total > 1 && deletedWasDefault)
        {
            var newest = await _db.AppDbSnapshots
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (newest is not null)
            {
                await _db.AppDbSnapshots.ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.IsDefault, false));

                newest.IsDefault = true;
                await _db.SaveChangesAsync();
            }
        }

        return Ok(new { deleted = true, id });
    }

    public record SaveSnapshotRequest(
    string? Label,
    bool SetDefault,
    string Json
    );
}
