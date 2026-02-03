using Astrodaiva.Data.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Astrodaiva.Data.Models
{
    public partial class PlanetInZodiac : ObservableObject
    {
        [ObservableProperty]
        private Planet planet;

        private ZodiacSign newZodiacSign;
        private ZodiacSign previousZodiacSign;

        private bool isRetrograde;
        private bool isInitialized;

        public ZodiacSign NewZodiacSign
        {
            get => newZodiacSign;
            set
            {
                if (SetProperty(ref newZodiacSign, value))
                {
                    if (isInitialized)
                    {
                        UpdatePreviousZodiacSign();
                    }
                }
            }
        }

        public ZodiacSign PreviousZodiacSign
        {
            get => previousZodiacSign;
            private set => SetProperty(ref previousZodiacSign, value);
        }
        
        public bool IsRetrograde
        {
            get => isRetrograde;
            set
            {
                if (SetProperty(ref isRetrograde, value) && isInitialized)
                {
                    UpdatePreviousZodiacSign();
                }
            }
        }

        [ObservableProperty]
        private bool isZodiacTransitioning;

        [ObservableProperty]
        private DateTime transitionTime;        

        public PlanetInZodiac()
        {
            isInitialized = true;
            UpdatePreviousZodiacSign();
        }

        public void UpdatePreviousZodiacSign()
        {
            if (isRetrograde)
            {
                if (newZodiacSign == ZodiacSign.Pisces)
                {
                    PreviousZodiacSign = ZodiacSign.Aries;
                }
                else
                {
                    PreviousZodiacSign = (ZodiacSign)((int)newZodiacSign + 1);
                }
            }
            else
            {
                if (newZodiacSign == ZodiacSign.Aries)
                {
                    PreviousZodiacSign = ZodiacSign.Pisces;
                }
                else
                {
                    PreviousZodiacSign = (ZodiacSign)((int)newZodiacSign - 1);
                }
            }
        }
    }
}
