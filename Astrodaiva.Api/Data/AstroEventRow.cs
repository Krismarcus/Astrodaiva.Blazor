using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astrodaiva.Api.Data;

[Table("astro_events")]
public class AstroEventRow
{
    [Key]
    [Column(TypeName = "date")]
    public DateTime Date { get; set; }

    public int MoonPhase { get; set; }

    public bool SunEclipse { get; set; }
    public bool MoonEclipse { get; set; }

    public ActivityQuality Barber { get; set; }
    public ActivityQuality Beauty { get; set; }
    public ActivityQuality BuyStuff { get; set; }
    public ActivityQuality Contracts { get; set; }
    public ActivityQuality ImportantTasks { get; set; }
    public ActivityQuality Gardening { get; set; }
    public ActivityQuality Love { get; set; }
    public ActivityQuality Meetings { get; set; }
    public ActivityQuality NewIdeas { get; set; }
    public ActivityQuality Tech { get; set; }
    public ActivityQuality Travel { get; set; }

    [Column(TypeName = "longtext")]
    public string? EventText { get; set; }

    // MoonDay (flattened)
    public int MoonDayNew { get; set; }
    public int MoonDayMiddle { get; set; }
    public int MoonDayPrevious { get; set; }
    public bool MoonDayIsTriple { get; set; }
    public DateTime MoonDayTransitionTime { get; set; }
    public DateTime MoonDayMiddleTransitionTime { get; set; }
}
