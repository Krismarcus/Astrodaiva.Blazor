using System.Text.Json;
using Astrodaiva.Data.Models;

namespace Astrodaiva.Blazor.Services
{
    public class AstroDbEditService
    {
        private readonly AstroDbStore _store;

        public AstroDbEditService(AstroDbStore store) => _store = store;

        public AppDB? Db => _store.Db;

        public bool IsLoaded => Db is not null;

        // ALWAYS load fresh JSON from local wwwroot (dev fallback)
        public async Task<AppDB?> ForceReloadAsync() => await _store.ReloadFromLocalAsync();

        // Load only if not loaded yet
        public async Task<AppDB?> EnsureLoadedAsync() => await _store.EnsureLoadedAsync();

        // Replace DB with revert snapshot
        public void ReplaceDb(AppDB newDb) => _store.SetDb(newDb);

        // Clear DB when leaving Admin page
        public void ClearDb() { /* keep state app-wide */ }

        // Save the current in-memory DB to JSON file (dev-only; in production prefer DB snapshots)
        public async Task SaveAsync()
        {
            // NOTE: Blazor WASM cannot write to server disk.
            // Keep method for future server-hosted scenarios; for now, no-op.
            await Task.CompletedTask;
        }
    }
}
