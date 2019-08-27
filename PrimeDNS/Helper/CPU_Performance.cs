/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS.Helper
{
    using System.Diagnostics;

    internal class CpuPerformance
    {
        /*
        Call this method every time you need to know
        the current cpu usage.
        */

        private static PerformanceCounter _cpuCounter;
        private static PerformanceCounter _ramCounter;

        public static float GetCurrentCpuUsage()
        {
            _cpuCounter = new PerformanceCounter
            {
                CategoryName = "Process",
                CounterName = "% Processor Time",
                InstanceName = Process.GetCurrentProcess().ProcessName
            };
            var _cpuUsage = _cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000);
            _cpuUsage = _cpuCounter.NextValue();
            return _cpuUsage;
        }

        /*
        Call this method every time you need to get
        the amount of the available RAM in Mb
        */
        public static float GetAvailableRam()
        {
            _ramCounter = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName);
            return _ramCounter.NextValue();
        }

        public static float GetRamUsage()
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
