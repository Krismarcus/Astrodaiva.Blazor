using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astrodaiva.Api.Data;

[Table("moon_day_details")]
public class MoonDayDetailsRow
{
    [Key]
    public int MoonDay { get; set; }

    [Column(TypeName = "longtext")]
    public string MoonDayInfo { get; set; } = "";
}
