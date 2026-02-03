using System.ComponentModel.DataAnnotations.Schema;

namespace Astrodaiva.Api.Data;

[Table("planet_in_zodiac_details")]
public class PlanetInZodiacDetailsRow
{
    public Planet Planet { get; set; }
    public ZodiacSign ZodiacSign { get; set; }

    [Column(TypeName = "longtext")]
    public string PlanetInZodiacInfo { get; set; } = "";
}
