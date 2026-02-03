using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrodaiva.Data.Models
{
    public partial class AstroDayDetails : ObservableObject
    {
        [ObservableProperty]
        private string sunInZodiacDetails;
        [ObservableProperty]
        private string moonInZodiacDetails;
        [ObservableProperty]
        private string mercuryInZodiacDetails;
        [ObservableProperty]
        private string venusInZodiacDetails;
        [ObservableProperty]
        private string marsInZodiacDetails;
        [ObservableProperty]
        private string previousMoonDayDetails;
        [ObservableProperty]
        private string middleMoonDayDetails;
        [ObservableProperty]
        private string newMoonDayDetails;
    }
}
