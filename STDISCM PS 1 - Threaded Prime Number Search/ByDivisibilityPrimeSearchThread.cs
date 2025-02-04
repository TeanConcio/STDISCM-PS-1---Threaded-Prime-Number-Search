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
            int numberToCheck = PrimeSearch.NumberToCheck;
            int divisorToCheck = PrimeSearch.GetNextDivisorToCheck();
            while (numberToCheck > 0 || divisorToCheck > 0)
            {
                // If reach the end of the list, announce prime
                if (divisorToCheck < 2)
                {
                    // Check if still available to announce prime
                    if (PrimeSearch.LastThreadChecked == -1)
                    {
                        // Get timestamp and set the last thread checked
                        PrimeSearch.LastThreadChecked = ID;
                        PrimeCheckedMilliTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    }
                }

                // If the number is divisible by the divisor or is less than 2, it is not prime
                else if (numberToCheck < 2 || numberToCheck % divisorToCheck == 0)
                {
                    // Get the next number to check
                    PrimeSearch.GetNextNumberToCheck();
                }

                numberToCheck = PrimeSearch.NumberToCheck;
                divisorToCheck = PrimeSearch.GetNextDivisorToCheck();
            }
        }
    }
}
