using Astrodaiva.Data.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrodaiva.Data.Models
{
    public partial class ActivityType : ObservableObject
    {
        [ObservableProperty]
        private ActivitySymbol activityName;
        [ObservableProperty]
        private ActivityQuality activityQuality;
    }
}
