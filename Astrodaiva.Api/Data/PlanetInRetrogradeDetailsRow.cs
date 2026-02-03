using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astrodaiva.Api.Data;

[Table("planet_in_retrograde_details")]
public class PlanetInRetrogradeDetailsRow
{
    [Key]
    public Planet PlanetInRetrograde { get; set; }

    [Column(TypeName = "longtext")]
    public string PlanetInRetrogradeInfo { get; set; } = "";
}
