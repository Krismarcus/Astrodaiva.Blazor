using Astrodaiva.Data.Models;

namespace Astrodaiva.Blazor.Integration;

public static class CalendarVisibility
{
    public static int[] Years(AppDB? db, bool preview = false) => db?.AstroEventsDB?
        .Select(day => day.Date.Year).Distinct().Where(year => preview || !db.HiddenYears.Contains(year))
        .OrderBy(year => year).ToArray() ?? Array.Empty<int>();

    public static AppDB PublicCopy(AppDB draft)
    {
        var copy = CalendarImporter.Clone(draft);
        copy.AstroEventsDB = new(copy.AstroEventsDB.Where(day => !copy.HiddenYears.Contains(day.Date.Year)));
        copy.AstronomyContext.RemoveAll(day => copy.HiddenYears.Contains(day.Date.Year));
        return copy;
    }

    public static DateTime Nearest(DateTime date, int[] years)
    {
        if (years.Length == 0 || years.Contains(date.Year)) return date;
        var year = years.OrderBy(year => Math.Abs(year - date.Year)).ThenBy(year => year).First();
        return new DateTime(year, date.Month, Math.Min(date.Day, DateTime.DaysInMonth(year, date.Month)));
    }

    public static DateTime? Move(DateTime date, int direction, int[] years, bool byMonth)
    {
        if (direction < 0 && (byMonth ? date.Year == 1 && date.Month == 1 : date.Date == DateTime.MinValue.Date)) return null;
        if (direction > 0 && (byMonth ? date.Year == 9999 && date.Month == 12 : date.Date == DateTime.MaxValue.Date)) return null;
        var next = byMonth ? new DateTime(date.Year, date.Month, 1).AddMonths(direction) : date.AddDays(direction);
        if (years.Contains(next.Year)) return next;
        var candidates = years.Where(year => direction > 0 ? year > date.Year : year < date.Year);
        if (!candidates.Any()) return null;
        var targetYear = direction > 0 ? candidates.Min() : candidates.Max();
        return direction > 0 ? new DateTime(targetYear, 1, 1) : new DateTime(targetYear, 12, byMonth ? 1 : 31);
    }
}
