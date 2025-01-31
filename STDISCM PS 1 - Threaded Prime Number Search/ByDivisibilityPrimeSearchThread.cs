using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STDISCM_PS_1___Threaded_Prime_Number_Search
{

    internal class ByDivisibilityPrimeSearchThread : PrimeSearchThread
    {
        public long PrimeCheckedMilliTime { get; set; } = 0;

        public ByDivisibilityPrimeSearchThread(int id) : base(id)
        {
        }

        override public void Run()
        {
            int numberToCheck = PrimeSearch.GetNextNumberToCheck();
            int divisorToCheck = PrimeSearch.GetNextDivisorToCheck();
            while (numberToCheck > 0 || divisorToCheck > 0)
            {
                if (divisorToCheck < 2)
                {

                    // Get timestamp and set the last thread checked
                    PrimeSearch.LastThreadChecked = ID;
                    PrimeCheckedMilliTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                }

                numberToCheck = PrimeSearch.GetNextNumberToCheck();
                divisorToCheck = PrimeSearch.GetNextDivisorToCheck();
            }
        }
    }
}
