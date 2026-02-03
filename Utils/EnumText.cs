using System;

namespace Astrodaiva.Blazor.Utils
{
    public static class EnumText
    {
        public static string GetName<TEnum>(int value) where TEnum : struct, Enum
        {
            var t = typeof(TEnum);
            return Enum.IsDefined(t, value) ? Enum.GetName(t, value)! : "Unknown";
        }
    }
}