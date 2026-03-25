using System.Globalization;

namespace Astrodaiva.Blazor.Utils
{
    public static class Css
    {
        public static string Pct(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        public static string Px(double value) => value.ToString("0.##", CultureInfo.InvariantCulture) + "px";
    }
}
