using Astrodaiva.Data.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrodaiva.Data.Models
{
    public partial class PlanetInZodiacDetails : ObservableObject
    {
        [ObservableProperty]
        private Planet planet;
        [ObservableProperty]
        private ZodiacSign zodiacSign;
        [ObservableProperty]
        private string planetInZodiacInfo;
    }
}
