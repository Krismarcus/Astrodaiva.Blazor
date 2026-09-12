# CelestialMe month import

The Blazor admin page imports the selected calendar month into an editable draft. Import previews are held in the administrator's current browser session; they do not create a published snapshot. Use Backups to save a private server copy of a draft or Publish changes to make it public. Manual astronomy changes are retained on later imports until the administrator selects Reset this month’s manual astronomy overrides to API values. Text, activity ratings and interpretation libraries are always preserved by an import.

## Backend configuration

Set these in the backend hosting environment, never in Blazor's wwwroot configuration:

```
CelestialMe__BaseUrl=https://diamonds-office-pc.tailbb41ad.ts.net:10000/celestialme-api/
CelestialMe__ApiKey=<service credential from your secret manager>
CelestialMe__ProviderPlaceId=593116
```

ProviderPlaceId defaults to Vilnius (593116). This release accepts the Europe/Vilnius calculation zone. The actual coordinates returned by GeoNames are shown in the import preview. The supplied private-test credential expires on 2026-09-25 at 23:59:59 UTC; replace it before expiry. No service credential is included in source or frontend assets.

The current hosting setup uses an ASP.NET Core API and MySQL, with a separate Blazor WebAssembly frontend. Deploy both updated projects together. The frontend needs the new API routes and ETag header for publishing; a missing ETag cannot authorize a publish. Preserve the existing database connection and Admin settings. No database schema migration is required for this release: imported astronomy, provenance, boundary context and override baselines are retained in the versioned JSON snapshots.

Allow the frontend origin in Cors:AllowedOrigins. The default list now includes https://app.astrodaiva.com. ETag, Retry-After and X-Upstream-Request-Id are exposed through CORS. Configure any gateway in front of the backend to allow at least three minutes for an import response. Ordinary calendar reads continue to use stored data and do not call CelestialMe.

## Routes and access

- POST /api/astronomy/month accepts `{ "year": 2026, "month": 9 }` with an admin token. The backend supplies the service key and location. It fetches the selected month plus two dates on each side to cover visitor time-zone boundaries. The frontend applies only the selected month's astronomy to editable dates; adjacent data is retained separately as display context.
- GET /api/import/default remains public and returns an ETag representing the published payload. If no snapshot has been published, it returns 404 with ETag `"none"`.
- POST /api/import/full-sync accepts the existing label/setDefault/json fields plus baseRevision. Publishing requires the revision from the currently loaded published calendar. A stale or missing revision returns 409. Non-default saves remain private and do not update the published revision.
- Snapshot list/get/delete and set-default now require an admin token. The first/only private snapshot is no longer automatically published. Deleting the published snapshot returns 409; restore a different one first.
- Restoring uses POST /api/import/snapshots/{id}/set-default with `{ "baseRevision": "..." }`. Publication and restore use database transactions. Older frontend saves that would remove astronomy metadata are rejected.

## Time-zone behavior

The public calendar keeps its original lunar-day timeline: only actual transition starts appear as HH:mm labels, with the lunar day contained within a three-day date assigned to MiddleMoonDay. A day with no transition displays one lunar-day icon and no time marker. Imported dates use the same New, Middle, Previous and transition fields in admin; detailed segment/event controls are collapsed under Advanced astronomy editing.

Show exact events on the calendar is an admin setting stored with the calendar snapshot and disabled by default. Publish changes applies this display preference; hiding the section keeps its stored data. Import and reset preserve this preference and editorial content.

Browser time-zone detection is automatic. Visitors can override it using Event time zone; the preference stays on their device. Exact events are grouped by their date in the selected zone. Lunar segments are clipped to real civil-day UTC boundaries, including daylight-saving days. Source calculations remain for Vilnius, and activity ratings, authored text, daily phase and planet-sign snapshots remain tied to the Vilnius calendar date. Personal moonrise calculations for a visitor's city are outside this release.

The admin edits in Europe/Vilnius. Ambiguous or nonexistent manual local times are rejected rather than guessed. Imported segments preserve the returned lunar-day numbers, including single-segment dates and the 29 → 1 → 2 sequence. Exact events have stable source IDs, and display uses UTC instants rather than the misleading local-offset spelling of some upstream `*Utc` fields. Legacy records without imported astronomy keep their original Vilnius display.

Manual override tracking is grouped by planet, lunar segments, phase, eclipse flags, or the exact-event collection. Editing a value in one group preserves that group on reimport while the other groups can refresh. An overridden planet's original ingress/station events are suppressed; an edited ingress time is displayed instead. Reset affects the selected month only. The raw source response is retained separately from the edited segments/events.

## Verification

Run:

```
dotnet build Astrodaiva.Blazor.csproj
dotnet build Astrodaiva.Api/Astrodaiva.Api.csproj
dotnet run --project Verification/Astrodaiva.Verification.csproj
```

Verification uses a checked-in public calculation fixture and an isolated in-memory SQLite database. It tests the actual controllers and middleware, import roundtrips, override preservation/reset, editorial preservation, time-zone date changes and boundaries, authentication, optimistic publication, and rollback. It does not contact production or modify MySQL. MySQL deployment smoke checks remain necessary for infrastructure-specific behavior.

The test service still has the hosting and calculation-profile limitations documented by its provider, including the Selena compatibility notice. This implementation displays pending compatibility notices in previews. Production availability, provider licensing readiness and credential rotation remain deployment responsibilities.
