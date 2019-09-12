/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Configuration;
    internal class AppConfig
    {
        public int HostFileUpdaterFrequencyInSeconds;
        public int MapUpdaterFrequencyInSeconds;
        public int WatcherFrequencyInSeconds;
        public int MaxNumberOfCriticalDomains;
        public int TimeToLiveThresholdInSeconds;

        //public string PrimeDnsDataHome;
        public string MapDatabasePath;
        public string MapDatabaseJournalPath;
        public string StateDatabasePath;
        public string MapConnectionString;
        public string StateConnectionString;
        public string HostFilePath;
        public string DomainsPath;
        public string PrimeDnsHome;
        public string PrimeDnsFiles;
        public string PrimeDnsDomainsFolder;
        public string PrimeDnsAppConfigFolder;
        public string PrimeDnsSectionBeginString;
        public string PrimeDnsSectionEndString;

        public bool DomainsWatcherEnabled;
        public bool AppConfigWatcherEnabled;
        public bool IsPrimeDnsEnabled;
        public bool LogTelemetryEnabled;
        public bool IsInformationLogEnabled;
        public bool IsWarningLogEnabled;
        public bool IsTtlUpdaterEnabled;
        public bool IsDomainsUpdaterEnabled;
        public bool IsErrorLogEnabled;

        public string DnsResolver;

        public int DefaultTimeToLiveInSeconds;
        public int TimeToLiveUpdaterFrequencyInSeconds;
        public int TtlUpdaterErrorLimit;
        public int ParallelDnsCallsLimit;
        public int ParallelTtlCallsLimit;

        public const string ConstTableNamePrimeDnsMap = "PrimeDnsMap";
        public const string ConstTableNamePrimeDnsState = "PrimeDnsState";
        public const string ConstPrimeDnsSectionCreated = "PrimeDnsSectionCreated";
        public const string ConstPrimeDnsMapCreated = "PrimeDnsMapCreated";
        public const string ConstPrimeDnsCriticalDomainsUpdated = "PrimeDnsCriticalDomainsUpdated";
        public const string ConstPrimeDnsMapUpdated = "PrimeDnsMapUpdated";

        public static IConfiguration Configuration { get; set; }
        public int GetConfig()
        {
            if (PrimeDns.PrimeDnsDataHome == null)
            {
                PrimeDns.Log._LogInformation("PrimeDnsDataHome == null : Data Folder hasn't been entered, so using current folder as default", Logger.Logger.ConstStartUp, null);
                PrimeDns.PrimeDnsDataHome = Directory.GetCurrentDirectory();
            }
            if (!Directory.Exists(PrimeDns.PrimeDnsDataHome))
            {
                return 0;
            }
            var builder = new ConfigurationBuilder()
                    .SetBasePath(PrimeDns.PrimeDnsDataHome + "//Files//")
                    .AddJsonFile("AppSettings.json");

            Configuration = builder.Build();
            HostFileUpdaterFrequencyInSeconds = Convert.ToInt32(Configuration["HostFileUpdaterFrequencyInSeconds"]);
            MapUpdaterFrequencyInSeconds = Convert.ToInt32(Configuration["MapUpdaterFrequencyInSeconds"]);
            WatcherFrequencyInSeconds = Convert.ToInt32(Configuration["WatcherFrequencyInSeconds"]);
            MaxNumberOfCriticalDomains = Convert.ToInt32(Configuration["MaxNumberOfCriticalDomains"]);
            TimeToLiveThresholdInSeconds = Convert.ToInt32(Configuration["TimeToLiveThresholdInSeconds"]);

            DefaultTimeToLiveInSeconds = Convert.ToInt32(Configuration["DefaultTimeToLiveInSeconds"]);
            TimeToLiveUpdaterFrequencyInSeconds = Convert.ToInt32(Configuration["TimeToLiveUpdaterFrequencyInSeconds"]);
            TtlUpdaterErrorLimit = Convert.ToInt32(Configuration["TtlUpdaterErrorLimit"]);
            ParallelDnsCallsLimit = Convert.ToInt32(Configuration["ParallelDnsCallsLimit"]);
            ParallelTtlCallsLimit = Convert.ToInt32(Configuration["ParallelTtlCallsLimit"]);

            DnsResolver = Configuration["DnsResolver"];           

            LogTelemetryEnabled = Convert.ToBoolean(Configuration["LogTelemetryEnabled"]);

            IsInformationLogEnabled = Convert.ToBoolean(Configuration["IsInformationLogEnabled"]);
            IsWarningLogEnabled = Convert.ToBoolean(Configuration["IsWarningLogEnabled"]);
            IsErrorLogEnabled = Convert.ToBoolean(Configuration["IsErrorLogEnabled"]);

            PrimeDnsHome = Directory.GetCurrentDirectory();
            PrimeDnsFiles = Directory.GetCurrentDirectory() + "\\Files\\";
            PrimeDnsDomainsFolder = PrimeDns.PrimeDnsDataHome + "\\Files\\";
            PrimeDnsAppConfigFolder = PrimeDns.PrimeDnsDataHome + "\\Files\\";
            MapDatabasePath = PrimeDnsFiles  + Configuration["MapDatabaseFileName"];
            MapDatabaseJournalPath = PrimeDnsFiles + Configuration["MapDatabaseJournalFileName"];
            StateDatabasePath = PrimeDnsFiles + Configuration["StateDatabaseFileName"];
            MapConnectionString = "Filename=" + MapDatabasePath; 
            StateConnectionString = "Filename=" + StateDatabasePath;
            HostFilePath = Configuration["HostFilePath"];
            DomainsPath = PrimeDnsDomainsFolder + Configuration["DomainsFileName"];
            PrimeDnsSectionBeginString = Configuration["PrimeDNSSectionBeginString"];
            PrimeDnsSectionEndString = Configuration["primeDNSSectionEndString"];

            DomainsWatcherEnabled = Convert.ToBoolean(Configuration["DomainsWatcherEnabled"]);
            AppConfigWatcherEnabled = Convert.ToBoolean(Configuration["AppConfigWatcherEnabled"]);
            IsPrimeDnsEnabled = Convert.ToBoolean(Configuration["IsPrimeDNSEnabled"]);
            IsTtlUpdaterEnabled = Convert.ToBoolean(Configuration["IsTtlUpdaterEnabled"]);
            IsDomainsUpdaterEnabled = Convert.ToBoolean(Configuration["IsDomainsUpdaterEnabled"]);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            return 1;

        }

        internal void CallAppConfigWatcher(DateTimeOffset time)
        {
            PrimeDns.Log._LogInformation("AppConfig Watcher Started at Time : " + time.ToString(), Logger.Logger.ConstStartUp, null);
            Telemetry.Telemetry.PushStatusOfThread("AppConfigWatcher", "Started");
            PrimeDns.Config.GetConfig();
            PrimeDns.HostFileUpdater.HostFileConfigUpdater();
            PrimeDns.MapUpdater.MapConfigUpdater();
            Telemetry.Telemetry.PushStatusOfThread("AppConfigWatcher", "Ended");
            PrimeDns.Log._LogInformation("AppConfig Watcher Exited at Time : " + time.ToString(), Logger.Logger.ConstStartUp, null);

        }

        /*
         * ConfigChangeHandler() is called whenever a change event is triggered by AppSettings.json Watcher.
         */
        public void ConfigChangeHandler(object source, FileSystemEventArgs e)
        {
            PrimeDns.Log._LogInformation("CHANGE DETECTED IN APP CONFIG FILE!!!", Logger.Logger.ConstConfigWatcher, null);
            PrimeDns.Config.GetConfig();
            PrimeDns.HostFileUpdater.HostFileConfigUpdater();
            PrimeDns.MapUpdater.MapConfigUpdater();
        }

        /*
         * WatchAppConfig() creates AppSettings.json Watcher.
         */
        public void WatchAppConfig()
        {
            PrimeDns.ConfigWatcher = new FileSystemWatcher
            {
                Path = PrimeDnsAppConfigFolder,
                NotifyFilter = NotifyFilters.Attributes |
                                         NotifyFilters.CreationTime |
                                         NotifyFilters.FileName |
                                         NotifyFilters.LastAccess |
                                         NotifyFilters.LastWrite |
                                         NotifyFilters.Size |
                                         NotifyFilters.Security,
                Filter = "AppSettings.json"
            };
            PrimeDns.ConfigWatcher.Changed += new FileSystemEventHandler(ConfigChangeHandler);
            PrimeDns.ConfigWatcher.Created += new FileSystemEventHandler(ConfigChangeHandler);
            PrimeDns.ConfigWatcher.EnableRaisingEvents = true;
        }
    }
}
