using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrodaiva.Data.Models
{
    public class MoonDay : INotifyPropertyChanged
    {
        private int newMoonDay;
        private int middleMoonDay;
        private int previousMoonDay;
        private bool isTripleMoonDay;
        private DateTime transitionTime;
        private DateTime middleMoonDayTransitionTime;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsTripleMoonDay
        {
            get => isTripleMoonDay;
            set
            {
                if (isTripleMoonDay != value)
                {
                    isTripleMoonDay = value;
                    OnPropertyChanged(nameof(IsTripleMoonDay));
                    AdjustDaysForTripleMoonDay();
                }
            }
        }

        public int NewMoonDay
        {
            get => newMoonDay;
            set
            {
                if (newMoonDay != value)
                {
                    newMoonDay = value;
                    OnPropertyChanged(nameof(NewMoonDay));

                    if (newMoonDay == 1)
                    {
                        PreviousMoonDay = 30;
                    }
                    else
                    {
                        PreviousMoonDay = newMoonDay - 1;
                    }

                    AdjustDaysForTripleMoonDay();
                }
            }
        }

        public int MiddleMoonDay
        {
            get => middleMoonDay;
            private set
            {
                if (middleMoonDay != value)
                {
                    middleMoonDay = value;
                    OnPropertyChanged(nameof(MiddleMoonDay));
                }
            }
        }

        public int PreviousMoonDay
        {
            get => previousMoonDay;
            private set
            {
                if (previousMoonDay != value)
                {
                    previousMoonDay = value;
                    OnPropertyChanged(nameof(PreviousMoonDay));
                }
            }
        }

        public DateTime TransitionTime
        {
            get => transitionTime;
            set
            {
                if (transitionTime != value)
                {
                    transitionTime = value;
                    OnPropertyChanged(nameof(TransitionTime));
                }
            }
        }

        public DateTime MiddleMoonDayTransitionTime
        {
            get => middleMoonDayTransitionTime;
            set
            {
                if (middleMoonDayTransitionTime != value)
                {
                    middleMoonDayTransitionTime = value;
                    OnPropertyChanged(nameof(MiddleMoonDayTransitionTime));
                }
            }
        }

        private void AdjustDaysForTripleMoonDay()
        {
            if (IsTripleMoonDay)
            {
                MiddleMoonDay = PreviousMoonDay;

                if (NewMoonDay == 1)
                {
                    PreviousMoonDay = 29; // Assuming a 30-day cycle
                }
                else if (NewMoonDay == 2)
                {
                    PreviousMoonDay = 30; // Assuming a 30-day cycle
                }
                else
                {
                    PreviousMoonDay = NewMoonDay - 2;
                }
            }
        }
    }
}

