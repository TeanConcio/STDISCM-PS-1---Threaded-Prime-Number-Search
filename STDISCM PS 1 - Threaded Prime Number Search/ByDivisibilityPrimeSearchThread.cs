using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STDISCM_PS_1___Threaded_Prime_Number_Search
{

    internal class ByDivisibilityPrimeSearchThread : PrimeSearchThread
    {
        public ThreadStatus Status { get; set; } = ThreadStatus.RUNNING;

        public ByDivisibilityPrimeSearchThread(int id) : base(id)
        {
        }

        override public void Run()
        {
            int numberToCheck;
            int divisorToCheck;

            // While there is still a number to check
            do
            {
                // Get the next number and divisor to check
                divisorToCheck = PrimeSearch.GetNextDivisorToCheck();
                numberToCheck = PrimeSearch.NumberToCheck;

                // If divisor to check is finished, wait until ready
                if (divisorToCheck == -2)
                {
                    Status = ThreadStatus.WAITING;
                    continue;
                }

                // If last to check, announce that the thread last checked the number
                if (numberToCheck >= 2 && divisorToCheck == -1)
                {
                    PrimeSearch.LastThreadChecked = ID;
                    PrimeSearch.LastCheckMilliTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                    Status = ThreadStatus.RUNNING;
                }

                // If the number is divisible by the divisor or is less than 2, it is not prime
                else if (numberToCheck < 2 || numberToCheck % divisorToCheck == 0)
                {
                    PrimeSearch.MultipleFound = true;
                    PrimeSearch.SetPrimeIndexToCheck(-2);

                    Status = ThreadStatus.RUNNING;
                }
            }
            while (numberToCheck > 0);

            Status = ThreadStatus.FINISHED;
        }
    }



    // Enum of status of the thread
    public enum ThreadStatus
    {
        RUNNING,
        WAITING,
        FINISHED
    }
}
