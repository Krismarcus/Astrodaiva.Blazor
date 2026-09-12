namespace Astrodaiva.Data.Models;

// Explicit values preserve real sequences such as 29 → 1 → 2 and single-segment dates.
public class MoonDay
{
    public int NewMoonDay { get; set; }
    public int MiddleMoonDay { get; set; }
    public int PreviousMoonDay { get; set; }
    public bool IsTripleMoonDay { get; set; }
    public DateTime TransitionTime { get; set; }
    public DateTime MiddleMoonDayTransitionTime { get; set; }

    public void UpdateLegacySequence()
    {
        MiddleMoonDay = IsTripleMoonDay ? (NewMoonDay == 1 ? 30 : NewMoonDay - 1) : 0;
        PreviousMoonDay = IsTripleMoonDay
            ? (NewMoonDay <= 2 ? NewMoonDay + 28 : NewMoonDay - 2)
            : (NewMoonDay == 1 ? 30 : NewMoonDay - 1);
    }
}
