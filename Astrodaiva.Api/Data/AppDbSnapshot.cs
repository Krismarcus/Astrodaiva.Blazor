using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astrodaiva.Api.Data;

[Table("appdb_snapshots")]
public class AppDbSnapshot
{
    [Key]
    public long Id { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public string? Label { get; set; }

    // If true, the client can use this snapshot as the app's startup JSON.
    public bool IsDefault { get; set; }

    [Column(TypeName = "json")]
    public string AppDbJson { get; set; } = "";
}
