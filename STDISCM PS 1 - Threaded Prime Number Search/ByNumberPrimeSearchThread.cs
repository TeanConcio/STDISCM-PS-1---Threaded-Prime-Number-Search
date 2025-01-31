using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STDISCM_PS_1___Threaded_Prime_Number_Search
{
    internal class ByNumberPrimeSearchThread : PrimeSearchThread
    {
        public ByNumberPrimeSearchThread(int id) : base(id)
        {
        }

        override public void Run()
        {
            int numberToCheck = PrimeSearch.GetNextNumberToCheck();
            while (numberToCheck > 0)
            {
                if (PrimeSearch.IsPrime(numberToCheck))
                {
                    // Get Current Time in Milliseconds
                    long milliTimeNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                    // Add Prime Number to Primes List
                    PrimeSearch.AddPrime(numberToCheck);

                    // If Print Mode is Immediate
                    if (PrimeSearch.PrintMode == PrintMode.IMMEDIATE)
                    {
                        Console.WriteLine($"Thread {ID} [{milliTimeNow - PrimeSearch.StartMilliTime} ms]: {numberToCheck} is prime");
                    }
                }

                numberToCheck = PrimeSearch.GetNextNumberToCheck();
            }
        }
    }
}
