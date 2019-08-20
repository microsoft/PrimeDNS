namespace PrimeDNS.Helper
{
    using System.Diagnostics;

    internal class CPU_Performance
    {
        /*
        Call this method every time you need to know
        the current cpu usage.
        */

        private static PerformanceCounter cpuCounter;
        private static PerformanceCounter ramCounter;

        public static float GetCurrentCpuUsage()
        {
            cpuCounter = new PerformanceCounter
            {
                CategoryName = "Process",
                CounterName = "% Processor Time",
                InstanceName = Process.GetCurrentProcess().ProcessName
            };
            var cpuUsage = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            cpuUsage = cpuCounter.NextValue();
            return cpuUsage;
        }

        /*
        Call this method every time you need to get
        the amount of the available RAM in Mb
        */
        public static float GetAvailableRAM()
        {
            ramCounter = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName);
            return ramCounter.NextValue();
        }

        public static float GetRAMUsage()
        {
            float ramUsage;
            using (var proc = Process.GetCurrentProcess())
            {
                ramUsage = proc.PrivateMemorySize64;
            }
            return ramUsage; 
        }

    }
}
