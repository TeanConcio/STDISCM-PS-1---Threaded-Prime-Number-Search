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
                // Sleep microseconds just in case
                PrimeSearch.SleepMicroseconds(25);

                if (PrimeSearch.MainIsProcessing || PrimeSearch.NumberIsComposite)
                {
                    Status = ThreadStatus.WAITING;
                    continue;
                }

                int divisorToCheck = PrimeSearch.GetNextDivisorToCheck();

                // If divisor to check is finished, wait until ready
                if (divisorToCheck == -2
                    //|| PrimeSearch.MultipleFound || 
                    //|| Status == ThreadStatus.BEING_PROCESSED
                    //|| PrimeSearch.CurrentlyProcessing
                    )
                {
                    Status = ThreadStatus.WAITING;
                    continue;
                }
                
                Status = ThreadStatus.RUNNING;

                PrimeSearch.NumberCheckLock.EnterReadLock();
                PrimeSearch.NumberIsCompositeLock.EnterUpgradeableReadLock();
                try
                {
                    // If the number is divisible by the divisor or is less than 2, it is not prime
                    if ((PrimeSearch.NumberToCheck < 2 || (divisorToCheck > 1 && PrimeSearch.NumberToCheck % divisorToCheck == 0)))
                    {
                        PrimeSearch.NumberIsCompositeLock.EnterWriteLock();
                        try
                        {
                            PrimeSearch.NumberIsComposite = true;
                        }
                        finally
                        {
                            PrimeSearch.NumberIsCompositeLock.ExitWriteLock();
                        }
                    }

                    // If last to check, announce that the thread last checked the number
                    else if (PrimeSearch.NumberToCheck >= 2 && divisorToCheck == -1)
                    {
                        PrimeSearch.LastThreadChecked = ID;
                        PrimeSearch.LastCheckTime = DateTime.Now;
                    }
                }
                finally
                {
                    PrimeSearch.NumberIsCompositeLock.ExitUpgradeableReadLock();
                    PrimeSearch.NumberCheckLock.ExitReadLock();
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
