# Astrodaiva.Api – Full Sync (Option A)

## What this API does
- `POST /api/import/full-sync?label=...`
  - Saves a full AppDB snapshot into `appdb_snapshots` (backup)
  - Upserts:
    - `AstroEventsDB` -> `astro_events`
    - planet states inside each day -> `astro_event_planets`
    - day aspects (`PlanetEvents`) -> `astro_planet_events`
    - `PlanetInZodiacsDB` -> `planet_in_zodiac_details`
    - `PlanetInRetrogradeDetailsDB` -> `planet_in_retrograde_details`
    - `MoonDayDetailsDB` -> `moon_day_details`

## Setup
1. Update `appsettings.json` ConnectionStrings:Default (replace password)
2. Run the API:
   - `dotnet run`
3. Open Swagger:
   - `/swagger`

## Notes
- Migrations are included and are applied automatically on startup.
- `AspectSymbol` is stored as int in DB (`astro_planet_events.AspectSymbol`).
