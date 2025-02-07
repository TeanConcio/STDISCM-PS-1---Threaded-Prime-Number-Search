using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STDISCM_PS_1___Threaded_Prime_Number_Search
{
    internal class ByRangePrimeSearchThread : PrimeSearchThread
    {
        public int StartNum { get; set; }
        public int EndNum { get; set; }

        public ByRangePrimeSearchThread(int id, int startNum = 0, int endNum = 0) : base(id)
        {
            this.StartNum = startNum;
            this.EndNum = endNum;
        }

        override public void Run()
        {
            for (int i = StartNum; i <= EndNum; i++)
            {
                if (PrimeSearch.IsPrime(i))
                {
                    // Get Current Time
                    long nanoTime = PrimeSearch.GetElapsedNanoTime();
                    DateTime currentTime = DateTime.Now;

                    // Add Prime Number to Primes List
                    PrimeSearch.AddPrime(i, ID, nanoTime);

                    // If Print Mode is Immediate
                    if (PrimeSearch.PrintMode == PrintMode.IMMEDIATE)
                    {
                        Console.WriteLine(PrimeSearch.FormatPrimeFoundLog(currentTime, ID, nanoTime, i));
                    }
                }
            }
        }
    }
}
