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
        public static int NumberToCheck { get; set; } = 1;
        public static readonly ReaderWriterLockSlim NumberCheckLock = new ReaderWriterLockSlim();
        public static int PrimeIndexToCheck { get; set; } = 0;          // -1 = last to check, -2 = finished
        private static readonly ReaderWriterLockSlim PrimeIndexCheckLock = new ReaderWriterLockSlim();
        public static bool NumberIsComposite { get; set; } = false;
        public static readonly ReaderWriterLockSlim NumberIsCompositeLock = new ReaderWriterLockSlim();
        public static int LastThreadChecked { get; set; } = -1;
        public static long LastCheckMilliTime { get; set; } = 0;
        public static bool MainIsProcessing { get; set; } = false;



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

                    if (NumThreads < 1)
                    {
                        Console.WriteLine("Error: Number of Threads must be greater than 0. Setting Number of Threads to 1");
                        NumThreads = 1;
                    }
                }
                else if (parts[0].Trim().ToUpper() == "PRIME_RANGE")
                {
                    PrimeRange = int.Parse(parts[1].Trim());

                    if (PrimeRange < 2)
                    {
                        Console.WriteLine("Error: Prime Range must be greater than 1. Setting Prime Range to 100");
                        PrimeRange = 100;
                    }

                    if (NumThreads > PrimeRange)
                    {
                        Console.WriteLine("Error: Number of Threads is greater than Prime Range. Setting Number of Threads to be equal to Prime Range");
                        NumThreads = PrimeRange;
                    }
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
                NumberToCheck = 1;
                while (NumberToCheck <= PrimeRange)
                {
                    // Reset values
                    NumberIsCompositeLock.EnterWriteLock();
                    PrimeIndexCheckLock.EnterWriteLock();
                    try
                    {
                        NumberIsComposite = false;
                        LastThreadChecked = -1;
                        PrimeIndexToCheck = 0;
                    }
                    finally
                    {
                        NumberIsCompositeLock.ExitWriteLock();
                        PrimeIndexCheckLock.ExitWriteLock();
                    }

                    MainIsProcessing = false;

                    PrimeSearch.SleepMicroseconds(10);

                    int counter = 0;    // Counter to check infinite loop

                    // While there are (any threads running AND no primes to check AND a last thread that checked it)  AND no multiple found, wait
                    while ((!(AllThreadsWaiting() && PrimeIndexToCheck == -2 && LastThreadChecked != -1) && 
                        !NumberIsComposite) 
                        || NumberCheckLock.IsReadLockHeld || PrimeIndexCheckLock.IsReadLockHeld || NumberIsCompositeLock.IsUpgradeableReadLockHeld
                        )
                    {
                        counter++;
                        if (counter > 100000000)
                        {
                            Console.WriteLine("Error: Infinite Loop. This shouldn't happen...");
                            return;
                        }

                        // Sleep microseconds just in case
                        PrimeSearch.SleepMicroseconds(10);
                    }

                    MainIsProcessing = true;

                    // If prime found
                    if (!NumberIsComposite)
                    {
                        // BS Fail safe I don't want to use
                        //if (NumberToCheck > 19 && (
                        //    NumberToCheck % 2 == 0 ||
                        //    NumberToCheck % 3 == 0 ||
                        //    NumberToCheck % 5 == 0 ||
                        //    NumberToCheck % 7 == 0 ||
                        //    NumberToCheck % 11 == 0 ||
                        //    NumberToCheck % 13 == 0 ||
                        //    NumberToCheck % 17 == 0 ||
                        //    NumberToCheck % 19 == 0
                        //    ))
                        //{
                        //    continue;
                        //}

                        // Add Prime Number to Primes List
                        AddPrime(NumberToCheck);

                        // If Print Mode is Immediate
                        if (PrintMode == PrintMode.IMMEDIATE)
                        {
                            Console.WriteLine($"Thread {LastThreadChecked} [{LastCheckMilliTime - StartMilliTime} ms]: {NumberToCheck} is prime");
                        }
                    }

                    // Increment Number to Check
                    NumberCheckLock.EnterWriteLock();
                    try
                    {
                        NumberToCheck++;
                    }
                    finally
                    {
                        NumberCheckLock.ExitWriteLock();
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
            PrimeIndexCheckLock.EnterWriteLock();
            PrimesListLock.EnterReadLock();
            try
            {
                // If prime index to check is last to check or finished, return finished
                if (PrimeIndexToCheck < 0 || PrimeIndexToCheck >= PrimesList.Count)
                {
                    PrimeIndexToCheck = -2;
                    return PrimeIndexToCheck;
                }

                int nextPrime = PrimesList.ElementAt(PrimeIndexToCheck);

                // If the square of the next prime is less than the current number checked
                if (nextPrime * nextPrime <= NumberToCheck)
                {
                    PrimeIndexToCheck++;
                    return nextPrime;
                }
                // If end of prime list, set to last to check
                else
                {
                    if (PrimeIndexToCheck != -1)
                    {
                        PrimeIndexToCheck = -1;
                        return PrimeIndexToCheck;
                    }
                    return -2; // Return finished if already set to -1
                }
            }
            finally
            {
                PrimesListLock.ExitReadLock();
                PrimeIndexCheckLock.ExitWriteLock();
            }
        }

        // Get next number to check
        public static int GetNextNumberToCheck()
        {
            NumberCheckLock.EnterWriteLock();
            try
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
            finally
            {
                NumberCheckLock.ExitWriteLock();
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

        // Sleep for microseconds
        public static void SleepMicroseconds(int microseconds)
        {
            int milliseconds = microseconds / 1000;
            int remainingMicroseconds = microseconds % 1000;

            if (milliseconds > 0)
            {
                Thread.Sleep(milliseconds);
            }

            if (remainingMicroseconds > 0)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedTicks < remainingMicroseconds * (TimeSpan.TicksPerMillisecond / 1000))
                {
                    // Busy wait
                }
            }
        }
    }
}
