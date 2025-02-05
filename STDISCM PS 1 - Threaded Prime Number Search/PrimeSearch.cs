using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace STDISCM_PS_1___Threaded_Prime_Number_Search
{
    internal enum PrintMode
    {
        NONE = 0,
        IMMEDIATE = 1,
        DELAYED = 2
    }

    internal enum ThreadTaskDivisionMode
    {
        BY_RANGE = 1,
        BY_DIVISIBILITY = 2,
        BY_NUMBER = 3
    }



    internal class PrimeSearch
    {
        public static int NumThreads { get; set; }
        public static int PrimeRange { get; set; }
        public static PrintMode PrintMode { get; set; }
        public static ThreadTaskDivisionMode ThreadTaskDivisionMode { get; set; }

        public static PrimeSearchThread[] ThreadsList { get; set; }
        public static long StartMilliTime { get; set; }
        public static SortedSet<int> PrimesList { get; set; } = new SortedSet<int>([2, 3, 5, 7]);   // Memoization
        private static readonly ReaderWriterLockSlim PrimesListLock = new ReaderWriterLockSlim();

        // For By Divisibility Task Division
        public static int NumberToCheck { get; set; } = 1;              // -1 = finished
        private static readonly object NumberCheckLock = new object();
        public static int PrimeIndexToCheck { get; set; } = 0;          // -1 = last to check, -2 = finished
        private static readonly object PrimeIndexCheckLock = new object();
        public static int LastThreadChecked { get; set; } = -1;
        public static long LastCheckMilliTime { get; set; } = 0;
        public static bool MultipleFound { get; set; } = false;
        public static bool CurrentlyProcessing { get; set; } = false;



        // Get prime search configurations from config.txt
        public static void GetConfig()
        {
            string[] lines = System.IO.File.ReadAllLines("config.txt");
            foreach (string line in lines)
            {
                string[] parts = line.Split("=");
                if (parts[0].Trim().ToUpper() == "NUM_THREADS")
                {
                    NumThreads = int.Parse(parts[1].Trim());
                }
                else if (parts[0].Trim().ToUpper() == "PRIME_RANGE")
                {
                    PrimeRange = int.Parse(parts[1].Trim());
                }
                else if (parts[0].Trim().ToUpper() == "PRINT_MODE")
                {
                    if (parts[1].Trim().ToUpper() == "NONE")
                    {
                        PrintMode = PrintMode.NONE;
                    }
                    else if (parts[1].Trim().ToUpper() == "IMMEDIATE")
                    {
                        PrintMode = PrintMode.IMMEDIATE;
                    }
                    else if (parts[1].Trim().ToUpper() == "DELAYED")
                    {
                        PrintMode = PrintMode.DELAYED;
                    }
                }
                else if (parts[0].Trim().ToUpper() == "THREAD_TASK_DIVISION_MODE")
                {
                    if (parts[1].Trim().ToUpper() == "BY_RANGE")
                    {
                        ThreadTaskDivisionMode = ThreadTaskDivisionMode.BY_RANGE;
                    }
                    else if (parts[1].Trim().ToUpper() == "BY_DIVISIBILITY")
                    {
                        ThreadTaskDivisionMode = ThreadTaskDivisionMode.BY_DIVISIBILITY;
                    }
                    else if (parts[1].Trim().ToUpper() == "BY_NUMBER")
                    {
                        ThreadTaskDivisionMode = ThreadTaskDivisionMode.BY_NUMBER;
                    }
                }
            }

            // Print Configurations
            Console.WriteLine($"NumThreads: {NumThreads}");
            Console.WriteLine($"PrimeRange: {PrimeRange}");

            switch (PrintMode)
            {
                case PrintMode.NONE:
                    Console.WriteLine("PrintMode: NONE");
                    break;
                case PrintMode.IMMEDIATE:
                    Console.WriteLine("PrintMode: IMMEDIATE");
                    break;
                case PrintMode.DELAYED:
                    Console.WriteLine("PrintMode: DELAYED");
                    break;
                default:
                    break;
            }

            switch (ThreadTaskDivisionMode)
            {
                case ThreadTaskDivisionMode.BY_RANGE:
                    Console.WriteLine("ThreadTaskDivisionMode: BY_RANGE");
                    break;
                case ThreadTaskDivisionMode.BY_DIVISIBILITY:
                    Console.WriteLine("ThreadTaskDivisionMode: BY_DIVISIBILITY");
                    break;
                case ThreadTaskDivisionMode.BY_NUMBER:
                    Console.WriteLine("ThreadTaskDivisionMode: BY_NUMBER");
                    break;
                default:
                    break;
            }

            Console.WriteLine();
        }

        // Initialize Threads
        public static void InitializeThreads()
        {
            ThreadsList = new PrimeSearchThread[NumThreads];

            switch (ThreadTaskDivisionMode)
            {
                // Split Task by Range
                case ThreadTaskDivisionMode.BY_RANGE:
                    {
                        int range = PrimeRange / NumThreads;
                        int startNum = 1;
                        int endNum = range;
                        int remainder = PrimeRange % NumThreads;

                        for (int i = 0; i < NumThreads; i++)
                        {
                            if (remainder > 0)
                            {
                                endNum++;
                                remainder--;
                            }
                            ThreadsList[i] = new ByRangePrimeSearchThread(i, startNum, endNum);
                            startNum = endNum + 1;
                            endNum = range + startNum - 1;
                        }

                        break;
                    }

                // Split Task by Divisibility
                case ThreadTaskDivisionMode.BY_DIVISIBILITY:
                    {
                        for (int i = 0; i < NumThreads; i++)
                        {
                            ThreadsList[i] = new ByDivisibilityPrimeSearchThread(i);
                        }
                        break;
                    }

                // Split Task by Number
                case ThreadTaskDivisionMode.BY_NUMBER:
                    {
                        for (int i = 0; i < NumThreads; i++)
                        {
                            ThreadsList[i] = new ByNumberPrimeSearchThread(i);
                        }

                        break;
                    }

                default:
                    break;
            }
        }

        // Start Threads
        public static void StartThreads()
        {
            // Get Current Time to Milliseconds
            StartMilliTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            foreach (PrimeSearchThread thread in ThreadsList)
            {
                thread.Start();
            }

            // If Thread Task Division Mode is By Divisibility
            if (ThreadTaskDivisionMode == ThreadTaskDivisionMode.BY_DIVISIBILITY)
            {
                // For each number to check
                for (NumberToCheck = 1; NumberToCheck <= PrimeRange; NumberToCheck++)
                {
                    // Reset values
                    LastThreadChecked = -1;
                    lock (PrimeIndexCheckLock)
                    {
                        PrimeIndexToCheck = 0;
                    }

                    //CurrentlyProcessing = false;

                    // Set all threads to running
                    foreach (ByDivisibilityPrimeSearchThread thread in ThreadsList)
                    {
                        thread.Status = ThreadStatus.RUNNING;
                    }

                    // While there are still threads running or primes to check AND no multiple found
                    while (!(AllThreadsWaiting() && PrimeIndexToCheck == -2) && !MultipleFound)
                    {
                    }

                    //CurrentlyProcessing = true;

                    // Set all threads to being processed
                    foreach (ByDivisibilityPrimeSearchThread thread in ThreadsList)
                    {
                        thread.Status = ThreadStatus.BEING_PROCESSED;
                    }

                    // If multiple found, go to next number
                    if (MultipleFound)
                    {
                        MultipleFound = false;
                    }
                    else
                    {
                        // Add Prime Number to Primes List
                        AddPrime(NumberToCheck);

                        // If Print Mode is Immediate
                        if (PrintMode == PrintMode.IMMEDIATE)
                        {
                            Console.WriteLine($"Thread {LastThreadChecked} [{LastCheckMilliTime - StartMilliTime} ms]: {NumberToCheck} is prime");
                        }
                    }
                }
            }
        }

        // Join Threads
        public static void JoinThreads()
        {
            foreach (PrimeSearchThread thread in ThreadsList)
            {
                thread.Join();
            }

            // If Print Mode is Delayed
            if (PrintMode == PrintMode.DELAYED)
            {
                // Get Current Time in Microseconds
                long microtimeNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                foreach (int prime in PrimesList)
                {
                    Console.WriteLine($"Main Thread [{microtimeNow - StartMilliTime} ms]: {prime} is prime");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Total Primes Found: {PrimesList.Count}");
        }

        // Check if a number is prime
        public static bool IsPrime(int number)
        {
            if (number < 2)
            {
                return false;
            }

            // Check if divisible by numbers in the prime list up to the square root of the number
            int lastPrimeChecked = 0;

            for (int i = 0; i < PrimesList.Count; i++)
            {
                int currentPrime;

                // Enter Read Lock
                PrimesListLock.EnterReadLock();
                try
                {
                    currentPrime = PrimesList.ElementAt(i);
                }
                finally
                {
                    PrimesListLock.ExitReadLock();
                }

                // If the square of the current prime is greater than the number
                // Or the difference between the current prime and previous prime is greater than PrimeRange / (NumThreads * NumThreads) (To prevent skipping primes by range division)
                if (currentPrime * currentPrime > number
                    || (i > 0 && currentPrime - lastPrimeChecked > PrimeRange / (NumThreads * NumThreads)))
                {
                    if (i > 0)
                    {
                        lastPrimeChecked++;
                    }
                    break;
                }

                if (number % currentPrime == 0)
                {
                    return false;
                }

                lastPrimeChecked = currentPrime;
            }

            // If there are no primes in the list
            if (lastPrimeChecked == 0)
            {
                lastPrimeChecked = 2;
            }

            // Check if divisible by numbers from the last prime checked to the square root of the number
            for (int i = lastPrimeChecked; i * i <= number; i++)
            {
                if (number % i == 0)
                {
                    return false;
                }
            }

            return true;
        }

        // Add prime to the sorted set
        public static void AddPrime(int prime)
        {
            //PrimesListLock.EnterUpgradeableReadLock();
            PrimesListLock.EnterWriteLock();
            try
            {
                PrimesList.Add(prime);
            }
            finally
            {
                PrimesListLock.ExitWriteLock();
                //PrimesListLock.ExitUpgradeableReadLock();
            }
        }

        // Get next divisor to check
        public static int GetNextDivisorToCheck()
        {
            lock (PrimeIndexCheckLock)
            {
                // If prime index to check is last to check or finished, return finished
                if (PrimeIndexToCheck < 0 || PrimeIndexToCheck >= PrimesList.Count)
                {
                    PrimeIndexToCheck = -2;
                    return -2;
                }

                int nextPrime;
                PrimesListLock.EnterReadLock();
                try
                {
                    nextPrime = PrimesList.ElementAt(PrimeIndexToCheck);
                }
                finally
                {
                    PrimesListLock.ExitReadLock();
                }
                PrimeIndexToCheck++;

                // If the square of the next prime is less than the current number checked
                if (nextPrime * nextPrime <= NumberToCheck)
                {
                    return nextPrime;
                }
                // If end of prime list, set to last to check
                else
                {
                    PrimeIndexToCheck = -1;
                    return -1;
                }
            }
        }

        // Get next number to check
        public static int GetNextNumberToCheck()
        {
            lock (NumberCheckLock)
            {
                if (NumberToCheck < PrimeRange && NumberToCheck > 0)
                {
                    NumberToCheck++;
                    return NumberToCheck;
                }
                else
                {
                    NumberToCheck = -1;
                    return -1;
                }
            }
        }

        // Check if all threads are waiting
        public static bool AllThreadsWaiting()
        {
            foreach (ByDivisibilityPrimeSearchThread thread in ThreadsList)
            {
                if (thread.Status != ThreadStatus.WAITING)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
