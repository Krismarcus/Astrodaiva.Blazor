using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrodaiva.Data.Models
{
    public partial class InfoScreen : ObservableObject
    {
        [ObservableProperty]
        private string header;
        [ObservableProperty]
        private string infoText;        
        [ObservableProperty]
        private DateTime transitionTime;
    }
}
