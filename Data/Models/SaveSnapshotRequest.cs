namespace Astrodaiva.Blazor.Data.Models
{
    public class SaveSnapshotRequest
    {
        public string? Label { get; set; }
        public bool SetDefault { get; set; }
        public string Json { get; set; } = "";
    }

}
