using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrodaiva.Data.Models
{
    public partial class MoonDayDetails : ObservableObject
    {
        [ObservableProperty]
        private int moonDay;
        [ObservableProperty]
        private string moonDayInfo;
    }
}
