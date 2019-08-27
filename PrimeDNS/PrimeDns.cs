/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS
{
    using DNS;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    internal class PrimeDns
    {
        public static FileSystemWatcher DomainsWatcher;
        public static FileSystemWatcher ConfigWatcher;
        public static Logger.Logger Log;
        public static AppConfig Config;
        public static Map.MapUpdater MapUpdater;
        public static HostFile.HostFileUpdater HostFileUpdater;
        public static TimeToLiveUpdater TtlUpdater;
        public static DomainsConfig DomainsConfig;

        public static SemaphoreSlim Semaphore;
        public static int TtlUpdaterErrorCount;
        public static CancellationToken DnsResolverCancellationToken;
        public static string PrimeDnsDataHome;

        private readonly int _dataExists;

        internal PrimeDns()
        {
            Config = new AppConfig();          
            Log = new Logger.Logger();
            _dataExists  = Config.GetConfig();
        }

        private static void Main(string[] args)
        {
            /*
             *  The following assignments is made to trigger the constructor of PrimeDNS class. It won't work without that.
             */
            var primeDns = new PrimeDns();

            PrimeDnsDataHome = (args.Length > 0) ? args[0] : null;

            if (primeDns._dataExists != 1)
            {
                Log._LogInformation("********* PRIMEDNS CLOSES DUE TO LACK OF DATA *********", Logger.Logger.ConstStartUp, null);
            }
            else
            {
                Semaphore = new SemaphoreSlim(1, 1);

                if (Config.IsPrimeDnsEnabled)
                {
                    Log._LogInformation("********* PRIMEDNS STARTS *********", Logger.Logger.ConstStartUp, null);

                    MapUpdater = new Map.MapUpdater();
                    HostFileUpdater = new HostFile.HostFileUpdater();

                    CleanUp.Clean();

                    TtlUpdater = new TimeToLiveUpdater();
                    DomainsConfig = new DomainsConfig();

                    if (Config.DomainsWatcherEnabled)
                        DomainsConfig.WatchDomainsConfig();

                    if (Config.AppConfigWatcherEnabled)
                        Config.WatchAppConfig();

                    try
                    {
                        Run().Wait();
                    }
                    catch (Exception e)
                    {
                        Log._LogError("********* PRIMEDNS CRASHES WITH ERROR!!!! *********", Logger.Logger.ConstStartUp, e);
                        //CleanUp.Clean();
                    }
                }
                else
                {
                    Log._LogInformation("********* PRIMEDNS IS NOT ENABLED *********", Logger.Logger.ConstStartUp, null);
                }
            }
             
        }

        private static async Task Run()
        {      
            try
            {
                var t = RunMapUpdater();
            }
            catch (AggregateException ae)
            {
                Log._LogError("Exception occured while running Map Updater as async task - ",Logger.Logger.ConstTaskException,ae);
            }
            try
            {
                await RunHostFileUpdater();
            }
            catch (AggregateException ae)
            {
                Log._LogError("Exception occured while running HostFile Updater as async task - ", Logger.Logger.ConstTaskException, ae);
            }
        }

        /*
         * RunMapUpdater() runs an infinite loop that calls UpdateMap() repeatedly on a set frequency.
         */
        private static async Task RunMapUpdater()
        {
            var nextStartTimeOfMapUpdater = DateTimeOffset.UtcNow;
            var nextStartTimeOfTtlUpdater = DateTimeOffset.UtcNow;
            var nextStartTimeOfWatcher = DateTimeOffset.UtcNow;

            while (true)
            {
                Map.MapUpdater.UpdateMap(nextStartTimeOfMapUpdater);

                var cpuUsage = Helper.CpuPerformance.GetCurrentCpuUsage();
                var ramUsage = Helper.CpuPerformance.GetRamUsage();

                Log._LogInformation("CPU Utilization of PrimeDNS is - " + cpuUsage, Logger.Logger.ConstStartUp, null);
                Telemetry.Telemetry.PushCpuData(cpuUsage);
                Log._LogInformation("RAM Usage of PrimeDNS is - " + ramUsage, Logger.Logger.ConstStartUp, null);
                Telemetry.Telemetry.PushRamData(ramUsage);

                nextStartTimeOfMapUpdater += TimeSpan.FromSeconds(MapUpdater.MapUpdaterFrequencyInSeconds);
                var delayMapUpdater = nextStartTimeOfMapUpdater - DateTimeOffset.UtcNow;
                if (delayMapUpdater > TimeSpan.Zero)
                    await Task.Delay(delayMapUpdater);           
                if( (nextStartTimeOfTtlUpdater <= nextStartTimeOfMapUpdater) && Config.IsTtlUpdaterEnabled)
                {
                    TtlUpdaterErrorCount = 0;
                    await TtlUpdater.UpdateTtl(nextStartTimeOfTtlUpdater);
                    nextStartTimeOfTtlUpdater += TimeSpan.FromSeconds(TimeToLiveUpdater.TimeToLiveUpdaterFrequencyInSeconds);
                }
                if( (nextStartTimeOfWatcher <= nextStartTimeOfMapUpdater) && Config.IsDomainsUpdaterEnabled)
                {

                    DomainsConfig.CallDomainsWatcher(nextStartTimeOfWatcher);
                    Config.CallAppConfigWatcher(nextStartTimeOfWatcher);
                    nextStartTimeOfWatcher += TimeSpan.FromSeconds(Config.WatcherFrequencyInSeconds);
                }
            }
        }

        /*
         * RunHostFileUpdater() runs an infinite loop that calls UpdateHostfile() repeatedly on a set frequency.
         */
        private static async Task RunHostFileUpdater()
        {
            var nextStartTimeOfHostFileUpdater = DateTimeOffset.UtcNow;
            while (true)
            {
                HostFileUpdater.UpdateHostfile(nextStartTimeOfHostFileUpdater);

                nextStartTimeOfHostFileUpdater += TimeSpan.FromSeconds(HostFileUpdater.HostFileUpdaterFrequencyInSeconds);
                var delayHostFileUpdater = nextStartTimeOfHostFileUpdater - DateTimeOffset.UtcNow;
                if (delayHostFileUpdater > TimeSpan.Zero)
                    await Task.Delay(delayHostFileUpdater);       
            }
        }
    }
}
