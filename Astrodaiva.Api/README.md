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
1. Configure `ConnectionStrings:Default`.
2. Configure admin auth for mutating commands:
   - `Admin__Password` or `ADMIN_PASSWORD`
   - optional `Admin__TokenSigningKey` or `ADMIN_TOKEN_SIGNING_KEY`
3. Run the API:
   - `dotnet run`
4. Open Swagger:
   - `/swagger`

## Notes
- Anonymous clients may use `GET` endpoints.
- `POST`, `PUT`, `PATCH`, and `DELETE` require `Authorization: Bearer <admin-token>`.
- Get an admin token from `POST /api/auth/admin/login` with the configured admin password.
- Migrations are included and are applied automatically on startup.
- `AspectSymbol` is stored as int in DB (`astro_planet_events.AspectSymbol`).
