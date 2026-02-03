using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astrodaiva.Api.Data;

[Table("astro_planet_events")]
public class AstroPlanetEventRow
{
    [Key]
    public long Id { get; set; }

    [Column(TypeName = "date")]
    public DateTime Date { get; set; }

    public Planet Planet1 { get; set; }
    public Planet Planet2 { get; set; }

    // Stored as int so you don't need AspectSymbol enum here.
    public int AspectSymbol { get; set; }
}
