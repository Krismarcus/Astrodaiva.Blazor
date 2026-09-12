using System.Collections.ObjectModel;

namespace Astrodaiva.Data.Models
{
    public class AppDB
    {
        public bool ShowExactEvents { get; set; }
        public List<AstroEvent> AstronomyContext { get; set; } = new();
        public ObservableCollection<AstroEvent> AstroEventsDB { get; set; }
        public ObservableCollection<PlanetInZodiacDetails> PlanetInZodiacsDB { get; set; }
        public ObservableCollection<PlanetInRetrogradeDetails> PlanetInRetrogradeDetailsDB { get; set; }
        public ObservableCollection<MoonDayDetails> MoonDayDetailsDB { get; set; }
    }
}
