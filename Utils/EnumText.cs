using System;
using System.Globalization;
using Astrodaiva.Data.Enums;

namespace Astrodaiva.Blazor.Utils
{
    public static class EnumText
    {
        private static bool IsLithuanian => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("lt", StringComparison.OrdinalIgnoreCase);

        public static string GetName<TEnum>(int value) where TEnum : struct, Enum
        {
            var t = typeof(TEnum);
            if (!Enum.IsDefined(t, value)) return "Unknown";

            if (t == typeof(ActivitySymbol)) return GetActivityName((ActivitySymbol)value);
            if (t == typeof(MoonDaySymbol)) return GetMoonDayName((MoonDaySymbol)value);
            if (t == typeof(Planet)) return GetPlanetName((Planet)value);
            if (t == typeof(ZodiacSign)) return GetZodiacName((ZodiacSign)value);
            if (t == typeof(ActivityQuality)) return GetActivityQualityName((ActivityQuality)value);
            if (t == typeof(MoonPhaseSymbol)) return GetMoonPhaseName((MoonPhaseSymbol)value);
            return Enum.GetName(t, value) ?? "Unknown";
        }

        public static string GetPlanetName(Planet planet) => IsLithuanian ? planet switch
        {
            Planet.Sun => "Saulė",
            Planet.Moon => "Mėnulis",
            Planet.Mercury => "Merkurijus",
            Planet.Venus => "Venera",
            Planet.Mars => "Marsas",
            Planet.Jupiter => "Jupiteris",
            Planet.Saturn => "Saturnas",
            Planet.Uranus => "Uranas",
            Planet.Neptune => "Neptūnas",
            Planet.Pluto => "Plutonas",
            Planet.Selena => "Selena",
            Planet.Lilith => "Lilit",
            Planet.Rahu => "Rahu",
            Planet.Ketu => "Ketu",
            _ => planet.ToString()
        } : planet.ToString();

        public static string GetPlanetLocative(Planet planet) => IsLithuanian ? planet switch
        {
            Planet.Sun => "Saulėje",
            Planet.Moon => "Mėnulyje",
            Planet.Mercury => "Merkurijuje",
            Planet.Venus => "Veneroje",
            Planet.Mars => "Marse",
            Planet.Jupiter => "Jupiteryje",
            Planet.Saturn => "Saturne",
            Planet.Uranus => "Urane",
            Planet.Neptune => "Neptūne",
            Planet.Pluto => "Plutone",
            Planet.Selena => "Selenoje",
            Planet.Lilith => "Lilit",
            Planet.Rahu => "Rahu",
            Planet.Ketu => "Ketu",
            _ => planet.ToString()
        } : planet.ToString();

        public static string GetZodiacName(ZodiacSign sign) => IsLithuanian ? sign switch
        {
            ZodiacSign.Aries => "Avinas",
            ZodiacSign.Taurus => "Jautis",
            ZodiacSign.Gemini => "Dvyniai",
            ZodiacSign.Cancer => "Vėžys",
            ZodiacSign.Leo => "Liūtas",
            ZodiacSign.Virgo => "Mergelė",
            ZodiacSign.Libra => "Svarstyklės",
            ZodiacSign.Scorpio => "Skorpionas",
            ZodiacSign.Sagittarius => "Šaulys",
            ZodiacSign.Capricorn => "Ožiaragis",
            ZodiacSign.Aquarius => "Vandenis",
            ZodiacSign.Pisces => "Žuvys",
            _ => sign.ToString()
        } : sign.ToString();

        public static string GetZodiacLocative(ZodiacSign sign) => IsLithuanian ? sign switch
        {
            ZodiacSign.Aries => "Avine",
            ZodiacSign.Taurus => "Jautyje",
            ZodiacSign.Gemini => "Dvyniuose",
            ZodiacSign.Cancer => "Vėžyje",
            ZodiacSign.Leo => "Liūte",
            ZodiacSign.Virgo => "Mergelėje",
            ZodiacSign.Libra => "Svarstyklėse",
            ZodiacSign.Scorpio => "Skorpione",
            ZodiacSign.Sagittarius => "Šaulyje",
            ZodiacSign.Capricorn => "Ožiaragyje",
            ZodiacSign.Aquarius => "Vandenyje",
            ZodiacSign.Pisces => "Žuvyse",
            _ => sign.ToString()
        } : sign.ToString();

        public static string GetMoonDayName(MoonDaySymbol symbol) => IsLithuanian ? symbol switch
        {
            MoonDaySymbol.None => "—",
            MoonDaySymbol.Lantern => "Žibintas",
            MoonDaySymbol.Whale => "Banginis",
            MoonDaySymbol.Leopard => "Leopardas",
            MoonDaySymbol.Tree => "Pažinimo medis",
            MoonDaySymbol.Unicorn => "Vienaragis",
            MoonDaySymbol.Rainbow => "Vaivorykštė",
            MoonDaySymbol.Rooster => "Gaidys",
            MoonDaySymbol.Phoenix => "Feniksas",
            MoonDaySymbol.Bat => "Šikšnosparnis",
            MoonDaySymbol.Fountain => "Fontanas",
            MoonDaySymbol.Crown => "Karūna",
            MoonDaySymbol.Bowl => "Taurė",
            MoonDaySymbol.Wheel => "Ratas",
            MoonDaySymbol.Trumpet => "Trimitas",
            MoonDaySymbol.Snake => "Gyvatė",
            MoonDaySymbol.Dove => "Balandis",
            MoonDaySymbol.Grapes => "Vynuogių kekė",
            MoonDaySymbol.Monkey => "Beždžionė prieš veidrodį",
            MoonDaySymbol.Spider => "Voras",
            MoonDaySymbol.Eagle => "Erelis",
            MoonDaySymbol.Horse => "Žirgų kaimenė",
            MoonDaySymbol.Elephant => "Dramblys",
            MoonDaySymbol.Crocodile => "Krokodilas",
            MoonDaySymbol.Bear => "Meška",
            MoonDaySymbol.Tortoise => "Vėžlys",
            MoonDaySymbol.Toad => "Varlė",
            MoonDaySymbol.Ship => "Laivas",
            MoonDaySymbol.Lotus => "Lotosas",
            MoonDaySymbol.Octopus => "Hidra",
            MoonDaySymbol.Swan => "Gulbė",
            _ => symbol.ToString()
        } : symbol.ToString();

        public static string GetActivityName(ActivitySymbol symbol) => IsLithuanian ? symbol switch
        {
            ActivitySymbol.Barber => "Kirpykla",
            ActivitySymbol.Beauty => "Grožis",
            ActivitySymbol.BuyStuff => "Pirkimai",
            ActivitySymbol.Contracts => "Sutartys",
            ActivitySymbol.ImportantTasks => "Svarbūs darbai",
            ActivitySymbol.Love => "Meilė",
            ActivitySymbol.Meetings => "Susitikimai",
            ActivitySymbol.NewIdeas => "Naujos idėjos",
            ActivitySymbol.Technologies => "Technologijos",
            ActivitySymbol.Travel => "Kelionės",
            _ => symbol.ToString()
        } : symbol.ToString();

        public static string GetActivityQualityName(ActivityQuality quality) => IsLithuanian ? quality switch
        {
            ActivityQuality.Neutral => "Neutralu",
            ActivityQuality.Good => "Palanku",
            ActivityQuality.Bad => "Nepalanku",
            ActivityQuality.None => "Nėra",
            _ => quality.ToString()
        } : quality.ToString();

        public static string GetMoonPhaseName(MoonPhaseSymbol phase) => IsLithuanian ? phase switch
        {
            MoonPhaseSymbol.None => "—",
            MoonPhaseSymbol.NewMoon => "Jaunatis",
            MoonPhaseSymbol.FirstQuarter => "Priešpilnis",
            MoonPhaseSymbol.FullMoon => "Pilnatis",
            MoonPhaseSymbol.ThirdQuarter => "Delčia",
            _ => phase.ToString()
        } : phase.ToString();

        public static string GetZodiacKey(ZodiacSign sign) => sign.ToString().ToLowerInvariant();
        public static string GetMoonDayKey(MoonDaySymbol symbol) => symbol.ToString().ToLowerInvariant();

        public static string In(Planet subject, ZodiacSign sign)
            => IsLithuanian
                ? $"{GetPlanetName(subject)} {GetZodiacLocative(sign)}"
                : $"{GetPlanetName(subject)} In {GetZodiacName(sign)}";

        public static string In(Planet subject, Planet target)
            => IsLithuanian
                ? $"{GetPlanetName(subject)} {GetPlanetLocative(target)}"
                : $"{GetPlanetName(subject)} In {GetPlanetName(target)}";
    }
}
