using System.ComponentModel.DataAnnotations.Schema;

namespace Astrodaiva.Api.Data;

[Table("astro_event_planets")]
public class AstroEventPlanetRow
{
    [Column(TypeName = "date")]
    public DateTime Date { get; set; }

    public Planet Planet { get; set; }

    public ZodiacSign NewZodiacSign { get; set; }
    public ZodiacSign PreviousZodiacSign { get; set; }
    public bool IsRetrograde { get; set; }
    public bool IsZodiacTransitioning { get; set; }
    public DateTime TransitionTime { get; set; }
}
