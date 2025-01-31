using System;
using System.Collections;
using System.Collections.Generic;
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

        private static PrimeSearchThread[] Threads;
        public static long StartMilliTime { get; set; }

        private static int CurrentPrimeListUsed = 1;       // For seamless prime list update for threading
        private static SortedSet<int> PrimesList1 = new SortedSet<int>([2, 3, 5, 7]);   // Memoization
        private static readonly ReaderWriterLockSlim PrimesListLock1 = new ReaderWriterLockSlim();
        private static SortedSet<int> PrimesList2 = new SortedSet<int>();   // For seamless prime list update for threading
        private static readonly ReaderWriterLockSlim PrimesListLock2 = new ReaderWriterLockSlim();

        private static int CurrentDivisorChecked = 2;       // For By Divisibility Task Division
        private static readonly object DivisorCheckLock = new object();

        private static int CurrentNumberChecked = 1;       // For By Number Task Division
        private static readonly object NumberCheckLock = new object();



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
            Threads = new PrimeSearchThread[NumThreads];

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
                            Threads[i] = new ByRangePrimeSearchThread(i, startNum, endNum);
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
                            Threads[i] = new ByDivisibilityPrimeSearchThread(i);
                        }
                        break;
                    }

                // Split Task by Number
                case ThreadTaskDivisionMode.BY_NUMBER:
                    {
                        for (int i = 0; i < NumThreads; i++)
                        {
                            Threads[i] = new ByNumberPrimeSearchThread(i);
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

            foreach (PrimeSearchThread thread in Threads)
            {
                thread.Start();
            }

            // If Thread Task Division Mode is By Divisibility
            if (ThreadTaskDivisionMode == ThreadTaskDivisionMode.BY_DIVISIBILITY)
            {
                
            }
        }

        // Join Threads
        public static void JoinThreads()
        {
            foreach (PrimeSearchThread thread in Threads)
            {
                thread.Join();
            }

            // Set current prime list to the prime list with more primes
            CurrentPrimeListUsed = PrimesList1.Count > PrimesList2.Count ? 1 : 2;

            // If Print Mode is Delayed
            if (PrintMode == PrintMode.DELAYED)
            {
                // Get Current Time in Microseconds
                long microtimeNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                foreach (int prime in GetPrimeList())
                {
                    Console.WriteLine($"Main Thread [{microtimeNow - StartMilliTime} ms]: {prime} is prime");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Total Primes Found: {GetPrimeList().Count}");
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

            for (int i = 0; i < GetPrimeList().Count; i++)
            {
                int currentPrime;

                //try { 
                //    currentPrime = GetPrimeList().ElementAt(i); 
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine(e.Message);
                //    return false;
                //}

                currentPrime = GetPrimeList().ElementAt(i);

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
            // Write lock both prime lists
            PrimesListLock1.EnterWriteLock();
            PrimesListLock2.EnterWriteLock();
            try
            {
                // Add prime to the unused prime list
                switch (CurrentPrimeListUsed)
                {
                    case 1:
                        PrimesList2.Add(prime);
                        break;
                    case 2:
                        PrimesList1.Add(prime);
                        break;
                    default:
                        break;
                }

                // If the prime list count difference is greater than PrimeRange / NumThreads
                if (Math.Abs(PrimesList1.Count - PrimesList2.Count) > 5)
                {
                    // Update the prime list not being used and switch to the updated prime list
                    switch (CurrentPrimeListUsed)
                    {
                        case 1:
                            PrimesList1.UnionWith(PrimesList2);
                            CurrentPrimeListUsed = 2;
                            break;
                        case 2:
                            PrimesList2.UnionWith(PrimesList1);
                            CurrentPrimeListUsed = 1;
                            break;
                        default:
                            break;
                    }
                }
            }
            finally
            {
                PrimesListLock1.ExitWriteLock();
                PrimesListLock2.ExitWriteLock();
            }
        }

        // Get prime list being used
        public static SortedSet<int> GetPrimeList()
        {
            switch (CurrentPrimeListUsed)
            {
                case 1:
                    return PrimesList1;
                case 2:
                    return PrimesList2;
                default:
                    return PrimesList1;
            }
        }

        // Get next divisor to check
        public static int GetNextDivisorToCheck()
        {
            lock (DivisorCheckLock)
            {
                if (CurrentDivisorChecked <= PrimeRange)
                {
                    return CurrentDivisorChecked++;
                }
                else
                {
                    return -1;
                }
            }
        }

        // Get next number to check
        public static int GetNextNumberToCheck()
        {
            lock (NumberCheckLock)
            {
                if (CurrentNumberChecked <= PrimeRange)
                {
                    return CurrentNumberChecked++;
                }
                else
                {
                    return -1;
                }
            }
        }
    }
}
