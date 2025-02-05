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
            while (PrimeSearch.NumberToCheck <= PrimeSearch.PrimeRange)
            {
                //if (PrimeSearch.CurrentlyProcessing)
                //{
                //    Status = ThreadStatus.WAITING;
                //    continue;
                //}

                //Status = ThreadStatus.RUNNING;

                int divisorToCheck = PrimeSearch.GetNextDivisorToCheck();

                // If divisor to check is finished, wait until ready
                if (PrimeSearch.MultipleFound || divisorToCheck == -2
                    || Status == ThreadStatus.BEING_PROCESSED
                    //|| PrimeSearch.CurrentlyProcessing
                    )
                {
                    Status = ThreadStatus.WAITING;
                    continue;
                }

                int numberToCheck = PrimeSearch.NumberToCheck;
                Status = ThreadStatus.RUNNING;

                // If last to check, announce that the thread last checked the number
                if (numberToCheck >= 2 && divisorToCheck == -1)
                {
                    PrimeSearch.LastThreadChecked = ID;
                    PrimeSearch.LastCheckMilliTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                }

                // If the number is divisible by the divisor or is less than 2, it is not prime
                else if (numberToCheck < 2 || numberToCheck % divisorToCheck == 0)
                {
                    PrimeSearch.MultipleFound = true;
                }
            }

            Status = ThreadStatus.FINISHED;
        }
    }



    // Enum of status of the thread
    public enum ThreadStatus
    {
        RUNNING,
        WAITING,
        BEING_PROCESSED,
        FINISHED
    }
}
