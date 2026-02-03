using Astrodaiva.Data.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrodaiva.Data.Models
{
    public partial class ZodiacSegment : ObservableObject
    {
        [ObservableProperty]
        public ZodiacSign zodiacSign;
        [ObservableProperty]
        DateTime zodiacStartDate;
        [ObservableProperty]
        DateTime zodiacEndDate;
        [ObservableProperty]
        private int duration;  // New property to track the number of days
    }
}
