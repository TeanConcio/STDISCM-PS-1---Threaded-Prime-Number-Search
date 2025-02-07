using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        // Configs
        public static int NumThreads { get; set; }
        public static int PrimeRange { get; set; }
        public static PrintMode PrintMode { get; set; }
        public static ThreadTaskDivisionMode ThreadTaskDivisionMode { get; set; }

        // Time keeping
        private static DateTime StartTime { get; set; }
        private static readonly Stopwatch NanoStopwatch = new Stopwatch();

        // Threads and Primes List
        public static PrimeSearchThread[] ThreadsList { get; set; }
        public static SortedSet<int> PrimesList { get; set; } = new SortedSet<int>([2, 3, 5, 7]);   // Memoization
        public static Dictionary<int, (int ThreadID, long NanoTimestamp)> PrimeDetails { get; set; } = new Dictionary<int, (int ThreadID, long NanoTimestamp)>();
        private static readonly ReaderWriterLockSlim PrimesListLock = new ReaderWriterLockSlim();

        // For By Divisibility Task Division
        public static int NumberToCheck { get; set; } = 1;
        public static readonly ReaderWriterLockSlim NumberCheckLock = new ReaderWriterLockSlim();
        public static int PrimeIndexToCheck { get; set; } = 0;          // -1 = last to check, -2 = finished
        private static readonly ReaderWriterLockSlim PrimeIndexCheckLock = new ReaderWriterLockSlim();
        public static bool NumberIsComposite { get; set; } = false;
        public static readonly ReaderWriterLockSlim NumberIsCompositeLock = new ReaderWriterLockSlim();
        public static int LastThreadChecked { get; set; } = -1;
        public static long LastCheckNanoTime { get; set; }
        public static DateTime LastCheckTime { get; set; }
        public static bool MainIsProcessing { get; set; } = false;



        // Get prime search configurations from config.txt
        public static void GetConfig()
        {
            bool hasErrorWarning = false;

            string[] lines = System.IO.File.ReadAllLines("config.txt");
            foreach (string line in lines)
            {
                // Skip empty lines or comments (#)
                if (line.Trim() == "" || line.Trim().StartsWith("#"))
                {
                    continue;
                }

                // Split line by "="
                string[] parts = line.Split("=");

                if (parts[0].Trim().ToUpper() == "NUM_THREADS")
                {
                    if (!int.TryParse(parts[1].Trim(), out int numThreads) || numThreads < 1)
                    {
                        Console.WriteLine("Error: Invalid Number of Threads. Setting Number of Threads to 4.");
                        NumThreads = 4;
                        hasErrorWarning = true;
                    }
                    else
                    {
                        NumThreads = numThreads;

                        // Warning
                        if (NumThreads >= 50)
                        {
                            Console.WriteLine("Warning: Higher number of threads may cause the program to run slower or run out of memory, especially for BY_DIVISIBILITY, but suit yourself.");
                            hasErrorWarning = true;
                        }
                    }
                }
                else if (parts[0].Trim().ToUpper() == "PRIME_RANGE")
                {
                    if (!int.TryParse(parts[1].Trim(), out int primeRange) || primeRange < 1)
                    {
                        Console.WriteLine("Error: Invalid Prime Range. Setting Prime Range to 1000.");
                        PrimeRange = 1000;
                        hasErrorWarning = true;
                    }
                    else
                    {
                        PrimeRange = primeRange;

                        // Warning
                        if (PrimeRange >= 10_000_000)
                        {
                            Console.WriteLine("Warning: Higher prime range may cause the program to run longer or run out of memory, but suit yourself.");
                            hasErrorWarning = true;
                        }
                    }

                    // If Number of Threads is greater than Prime Range, set Number of Threads to be equal to Prime Range
                    if (NumThreads > PrimeRange)
                    {
                        Console.WriteLine("Error: Number of Threads is greater than Prime Range. Setting Number of Threads to be equal to Prime Range.");
                        NumThreads = PrimeRange;
                        hasErrorWarning = true;
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
                    else
                    {
                        Console.WriteLine("Error: Invalid Print Mode. Setting Print Mode to IMMEDIATE.");
                        PrintMode = PrintMode.IMMEDIATE;
                        hasErrorWarning = true;
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
                    else
                    {
                        Console.WriteLine("Error: Invalid Thread Task Division Mode. Setting Thread Task Division Mode to BY_RANGE.");
                        ThreadTaskDivisionMode = ThreadTaskDivisionMode.BY_RANGE;
                        hasErrorWarning = true;
                    }
                }
            }

            // If there is an error, print a line
            if (hasErrorWarning)
            {
                Console.WriteLine();
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
            // Get Start Time
            StartTime = DateTime.Now;
            NanoStopwatch.Restart();
            NanoStopwatch.Start();
            Console.WriteLine($"Start Time: {FormatDateTime(StartTime)} ({FormatNanoTime(GetElapsedNanoTime())} ns)\n");

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

                    PrimeSearch.SleepMicroseconds();

                    int counter = 0;    // Counter to check infinite loop

                    // While there are (any threads running AND no primes to check AND a last thread that checked it)  AND no multiple found, wait
                    while ((!(AreAllThreadsWaiting() && PrimeIndexToCheck == -2 && LastThreadChecked != -1) &&
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
                        PrimeSearch.SleepMicroseconds();
                    }

                    MainIsProcessing = true;

                    // If prime found
                    if (!NumberIsComposite)
                    {
                        // BS Fail safe I don't want to use
                        if (!IsPrimeFailSafe(NumberToCheck))
                        {
                            continue;
                        }

                        // Add Prime Number to Primes List
                        AddPrime(NumberToCheck, LastThreadChecked, LastCheckNanoTime);

                        // If Print Mode is Immediate
                        if (PrimeSearch.PrintMode == PrintMode.IMMEDIATE)
                        {
                            Console.WriteLine(FormatPrimeFoundLog(LastCheckTime, LastThreadChecked, LastCheckNanoTime, NumberToCheck));
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

            // Get End Time (old implementation)
            //long endNanoTime = GetElapsedNanoTime();
            //DateTime endTime = DateTime.Now;

            // If Print Mode is Delayed
            if (PrintMode == PrintMode.DELAYED)
            {
                foreach (int prime in PrimesList)
                {
                    if (prime <= PrimeRange)
                    {
                        //Console.WriteLine(FormatPrimeFoundLog(endTime, -1, endNanoTime, prime));

                        if (PrimeDetails.TryGetValue(prime, out var details))
                        {
                            Console.WriteLine(FormatPrimeFoundLog(DateTime.Now, details.ThreadID, details.NanoTimestamp, prime));
                        }
                    }
                }
            }

            // Get number of primes in Prime List that are less than or equal to Prime Range
            int primeCount = PrimesList.Count(prime => prime <= PrimeRange);
            if (primeCount > 0)
            {
                Console.WriteLine();
            }
            Console.WriteLine($"Total Primes Found: {primeCount}");

            // Print End Time
            Console.WriteLine($"\nEnd Time: {FormatDateTime(DateTime.Now)} ({FormatNanoTime(GetElapsedNanoTime())} ns)\n");
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
        public static void AddPrime(int prime, int threadID, long nanoTimestamp)
        {
            //PrimesListLock.EnterUpgradeableReadLock();
            PrimesListLock.EnterWriteLock();
            try
            {
                PrimesList.Add(prime);

                if (PrintMode == PrintMode.DELAYED)
                {
                    PrimeDetails.Add(prime, (threadID, nanoTimestamp));
                }
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
        public static bool AreAllThreadsWaiting()
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
        public static void SleepMicroseconds(int microseconds = 10)
        {
            int milliseconds = microseconds / 1000;
            int remainingMicroseconds = microseconds % 1000;

            if (milliseconds > 0)
            {
                Thread.Sleep(milliseconds);
            }

            if (remainingMicroseconds > 0)
            {
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedTicks < remainingMicroseconds * (TimeSpan.TicksPerMillisecond / 1000))
                {
                    // Busy wait
                }
            }
        }

        // Get Elapsed Nano Time
        public static long GetElapsedNanoTime()
        {
            return (long)(NanoStopwatch.ElapsedTicks * (1_000_000_000.0 / Stopwatch.Frequency));
        }

        // Format Time
        public static string FormatDateTime(DateTime time)
            => time.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        //=> time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

        // Format Nanoseconds
        public static string FormatNanoTime(long nanoTime)
            => nanoTime.ToString("N0", CultureInfo.InvariantCulture);

        // Format Prime Found Log
        public static string FormatPrimeFoundLog(DateTime time, int threadID, long nanoTime, int prime)
        {
            // If threadID is -1, it is the main thread
            if (threadID < 0)
            {
                return $"[{FormatDateTime(time)}] Main Thread ({FormatNanoTime(nanoTime)} ns) : {prime} is prime";
            }

            return $"[{FormatDateTime(time)}] Thread {threadID} ({FormatNanoTime(nanoTime)} ns) : {prime} is prime";
        }

        // Fail safe I don't want to use
        public static bool IsPrimeFailSafe(int number)
        {
            if (NumberToCheck > 19 && (
                NumberToCheck % 2 == 0 ||
                NumberToCheck % 3 == 0 ||
                NumberToCheck % 5 == 0 ||
                NumberToCheck % 7 == 0 ||
                NumberToCheck % 11 == 0 ||
                NumberToCheck % 13 == 0 ||
                NumberToCheck % 17 == 0 ||
                NumberToCheck % 19 == 0
                ))
            {
                return false;
            }

            return true;
        }
    }
}
