using System;
using System.Collections.Generic;
using System.Globalization;
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
                    // Get Current Time
                    long nanoTime = PrimeSearch.GetElapsedNanoTime();
                    DateTime currentTime = DateTime.Now;

                    // Add Prime Number to Primes List
                    PrimeSearch.AddPrime(numberToCheck);

                    // If Print Mode is Immediate
                    if (PrimeSearch.PrintMode == PrintMode.IMMEDIATE)
                    {
                        Console.WriteLine(PrimeSearch.FormatPrimeFoundLog(currentTime, ID, nanoTime, numberToCheck));
                    }
                }

                numberToCheck = PrimeSearch.GetNextNumberToCheck();
            }
        }
    }
}
