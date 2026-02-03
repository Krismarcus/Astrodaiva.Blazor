using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrodaiva.Data.Models
{
    public partial class RetrogradeSegment : ObservableObject
    {
        [ObservableProperty]
        bool isRetrograde;
        [ObservableProperty]
        DateTime retrogradeStartDate;
        [ObservableProperty]
        DateTime retrogradeEndDate;
        [ObservableProperty]
        private int duration;
    }
}
