
using Astrodaiva.Data.Enums;

namespace Astrodaiva.Blazor.Utils
{
    public static class Zodiac
    {
        private static readonly string[] Symbols = { "♈","♉","♊","♋","♌","♍","♎","♏","♐","♑","♒","♓" };
        private static int ToIndex(ZodiacSign sign)
        {
            var raw = (int)sign;
            var idx = (raw >= 1 && raw <= 12) ? raw - 1 : raw; // supports 1-based or 0-based enums
            return ((idx % 12) + 12) % 12;
        }

        public static string Symbol(ZodiacSign sign) => Symbols[ToIndex(sign)];
        public static string Name(ZodiacSign sign) => sign.ToString();

        public static string Symbol(int signIndex)
        {
            int i = ((signIndex % 12) + 12) % 12;
            return Symbols[i];
        }        

        public static string ImageFile(ZodiacSign sign) => sign switch
        {
            ZodiacSign.Aries => "aries_blue.png",
            ZodiacSign.Taurus => "taurus_blue.png",
            ZodiacSign.Gemini => "gemini_blue.png",
            ZodiacSign.Cancer => "cancer_blue.png",
            ZodiacSign.Leo => "leo_blue.png",
            ZodiacSign.Virgo => "virgo_blue.png",
            ZodiacSign.Libra => "libra_blue.png",
            ZodiacSign.Scorpio => "scorpio_blue.png",
            ZodiacSign.Sagittarius => "sagittarius_blue.png",
            ZodiacSign.Capricorn => "capricorn_blue.png",
            ZodiacSign.Aquarius => "aquarius_blue.png",
            ZodiacSign.Pisces => "pisces_blue.png",
            _ => "unknown.png"
        };

        public static string ImagePath(ZodiacSign sign) => $"img/zodiac/{sign.ToString().ToLowerInvariant()}.png";

        public static string Color(ZodiacSign sign) => sign switch
        {
            ZodiacSign.Aries => "#C22936",
            ZodiacSign.Taurus => "#C68813",
            ZodiacSign.Gemini => "#8F9131",
            ZodiacSign.Cancer => "#347288",
            ZodiacSign.Leo => "#C22936",
            ZodiacSign.Virgo => "#C68813",
            ZodiacSign.Libra => "#8F9131",
            ZodiacSign.Scorpio => "#347288",
            ZodiacSign.Sagittarius => "#C22936",
            ZodiacSign.Capricorn => "#C68813",
            ZodiacSign.Aquarius => "#8F9131",
            ZodiacSign.Pisces => "#347288",
            _ => "#CCCCCC"
        };
    }
}
