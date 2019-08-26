namespace PrimeDNS
{
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using DNS;
    using Map;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using Microsoft.Data.Sqlite;

    internal class DomainsConfig
    {
        private static string mapConnectionString;

        /*
         * _criticalDomains stores the Critical Domain names taken from Domains.json
         */
        private JToken[] _criticalDomains;

        /*
         * IsDomainCritical - True implies the Domain still exists in Domain.json
         * False or absence of domain name in the dictionary implies the domain names is no longer in Domains.json
         * This is mainly used by Domains.json watcher, to keep track of it's entries.
         */
        public Dictionary<string, bool> IsDomainCritical { get;  }
        public Dictionary<string, bool> DomainYetToBeAddedToMap { get; }

        /*
         * DomainsConfig() constructor initializes _criticalDomains and IsDomainCritical.
         */
        public DomainsConfig()
        {
            mapConnectionString = PrimeDns.Config.MapConnectionString;
            DomainYetToBeAddedToMap = new Dictionary<string, bool>();
            IsDomainCritical = new Dictionary<string, bool>();

            var domainsList = GetCriticalDomains();
            if(domainsList == null)
                return;
            UpdateIsDomainCriticalDictionary();
            Telemetry.Telemetry.PushNumberOfCriticalDomains(domainsList.Length);
        }

        private void CreateDomainCriticalListFromMap()
        {
            if (File.Exists(PrimeDns.Config.MapDatabasePath))
            {
                    string selectCommand = String.Format("Select hostname from " + AppConfig.CTableNamePrimeDnsMap);
                    SqliteDataReader query = null;

                    using (var Connection = new SqliteConnection(mapConnectionString))
                    {
                        Connection.Open();
                        using (var c = new SqliteCommand(selectCommand, Connection))
                        {
                            query = c.ExecuteReader();

                            while (query.Read())
                            {
                                IsDomainCritical.Add(query.GetString(0), true);
                            }
                        }
                        Connection.Close();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
        }

        internal void CallDomainsWatcher(DateTimeOffset time)
        {
            PrimeDns.Log._LogInformation("Domains Watcher Started at Time : " + time.ToString(), Logger.Logger.CStartUp, null);
            Telemetry.Telemetry.PushStatusOfThread("DomainsWatcher", "Started");
            var domainsList = GetCriticalDomains();
            if (domainsList == null)
                return;

            Task[] t = new Task[2]
                {
                    AddNewEntriesToPrimeDnsMap(),
                    Task.Delay(new TimeSpan(0, 0, 300))
                };

            int index = Task.WaitAny(t[0], t[1]);
            if (index == 1)
            {
                CancellationTokenSource source = new CancellationTokenSource();
                PrimeDns.DnsResolverCancellationToken = source.Token;
                source.Cancel();
                t[0].Wait();
            }

            UpdateIsDomainCriticalDictionary();
            Telemetry.Telemetry.PushNumberOfCriticalDomains(domainsList.Length);
            Telemetry.Telemetry.PushStatusOfThread("DomainsWatcher", "Ended");
            PrimeDns.Log._LogInformation("Domains Watcher Exited at Time : " + time.ToString(), Logger.Logger.CStartUp, null);
        }

        /*
         * DomainsChangeHandler() is called whenever a change event is triggered by Domains.json Watcher,
         * It first updates _criticalDomains by reading the changed file,
         * then it Adds the new entries detected to PrimeDNSMap,
         * finally, it updates IsDomainCritical dictionary.
         */
        public void DomainsChangeHandler(object source, FileSystemEventArgs e)
        {
            PrimeDns.Log._LogInformation("CHANGE DETECTED IN CRITICAL DOMAINS CONFIG FILE!!!", Logger.Logger.CDomainsWatcher, null);
            var domainsList = GetCriticalDomains();
            if (domainsList == null)
                return;
            AddNewEntriesToPrimeDnsMap().Wait();
            UpdateIsDomainCriticalDictionary();
            Telemetry.Telemetry.PushNumberOfCriticalDomains(domainsList.Length);
        }

        /*
         * WatchDomainsConfig() creates Domains.json watcher.
         */
        public void WatchDomainsConfig()
        {
            PrimeDns.DomainsWatcher = new FileSystemWatcher
            {
                Path = PrimeDns.Config.PrimeDnsDomainsFolder,
                NotifyFilter = NotifyFilters.Attributes |
                                NotifyFilters.CreationTime |
                                NotifyFilters.FileName |
                                NotifyFilters.LastAccess |
                                NotifyFilters.LastWrite |
                                NotifyFilters.Size |
                                NotifyFilters.Security,
                Filter = "Domains.json",
            };
            PrimeDns.DomainsWatcher.Changed += new FileSystemEventHandler(DomainsChangeHandler);
            PrimeDns.DomainsWatcher.Created += new FileSystemEventHandler(DomainsChangeHandler);
            PrimeDns.DomainsWatcher.EnableRaisingEvents = true;
        }

        /*
         * IsDomainNameValid() checks if the domain name passed is valid.
         */
        public static bool IsDomainNameValid(string pDomainName)
        {
            const string pattern = "^((?!-)[A-Za-z0-9-]{1,63}(?<!-)\\.)+[A-Za-z]{2,6}$";
            var validDomainNamePattern = new Regex(pattern);
            return validDomainNamePattern.IsMatch(pDomainName);
        }

        /*
         * AddNewEntriesToPrimeDnsMap() goes through _criticalDomains,
         * for each domain name, it checks whether it still exists in IsDomainCritical Dictionary,
         *      if yes and it's value is true, it means the domain name is not a new entry, so nothing to be done.
         *      if yes and it's value is false, it is absurd as no function marks them false and hence this is logged.
         *      if no, then it's a new entry,
         *          if the Domain Name is valid,
         *              DNS Resolver is called and the result is written to PrimeDNSMap,
         *              isDomainCritical() updated.
         *          if Domain Name is invalid, it's logged as a warning.
         */
        private async Task AddNewEntriesToPrimeDnsMap()
        {
            var tasks = new List<Task<Tuple<PrimeDnsMapRow, bool>>>();
            CancellationTokenSource source = new CancellationTokenSource();
            PrimeDns.DnsResolverCancellationToken = source.Token;

            foreach (string domain in _criticalDomains)
            {
                try
                {
                    var value = IsDomainCritical[domain];
                    if (!value)
                    {
                        PrimeDns.Log._LogInformation("Dictionary having a FALSE entry!!!!? " + domain, Logger.Logger.CDomainsWatcher, null);
                    }
                }
                catch (KeyNotFoundException)
                {
                    if (IsDomainNameValid(domain))
                    {
                        var mapRow = new PrimeDnsMapRow(domain);
                        tasks.Add(DoWorkAsync(mapRow, PrimeDns.DnsResolverCancellationToken));
                    }
                    else
                    {
                        PrimeDns.Log._LogWarning("Invalid Domain Name Found in File!", Logger.Logger.CDomainsWatcher, null);
                        Telemetry.Telemetry.PushDnsCallsData(domain, "Failure", "InvalidDomain", 0, 0, "INVALID-DOMAIN");
                    }

                    /*
                    if (tasks.Count > PrimeDns.Config.ParallelDnsCallsLimit)
                    {
                        foreach (var task in await Task.WhenAll(tasks))
                        {
                            if (task.Item2)
                            {
                                MapUpdater.WriteToPrimeDnsMap(task.Item1);
                                IsDomainCritical.Add(domain, true);
                                PrimeDns.Log._LogInformation("Added the New Domain to PrimeDNSMap " + domain, Logger.Logger.CDomainsWatcher, null);
                                //Console.WriteLine("Ending Dns Resolver {0}", task.Item1.HostName);
                            }
                        }
                        tasks.Clear();
                    }
                    */
                }
            }
            if (tasks.Count > 0)
            {
                foreach (var task in await Task.WhenAll(tasks))
                {
                    if (task.Item2)
                    {
                        try
                        {
                            if (DomainYetToBeAddedToMap[task.Item1.HostName])
                            {
                                PrimeDns.Log._LogInformation(" New Domain successfully added to PrimeDNSMap " + task.Item1.HostName, Logger.Logger.CDomainsWatcher, null);
                                DomainYetToBeAddedToMap.Remove(task.Item1.HostName);
                            }
                                
                        }
                        catch (KeyNotFoundException)
                        {
                            PrimeDns.Log._LogInformation("Added the New Domain to PrimeDNSMap " + task.Item1.HostName, Logger.Logger.CDomainsWatcher, null);
                        }
                        MapUpdater.WriteToPrimeDnsMap(task.Item1);
                        IsDomainCritical.Add(task.Item1.HostName, true);                     
                        //Console.WriteLine("Ending Dns Resolver {0}", task.Item1.HostName);
                    }
                    else
                    {
                        PrimeDns.Log._LogInformation("Failure in adding New Domain to PrimeDNSMap " + task.Item1.HostName, Logger.Logger.CDomainsWatcher, null);
                        try
                        {
                            if (!DomainYetToBeAddedToMap[task.Item1.HostName])
                                DomainYetToBeAddedToMap[task.Item1.HostName] = true;
                        }
                        catch (KeyNotFoundException)
                        {
                            DomainYetToBeAddedToMap.Add(task.Item1.HostName, true);
                        }
                    }
                }
                tasks.Clear();
            }
        }

        /*
         * GetCriticalDomains(), returns the unique critical domain names from Domains.json,
         * while also setting _criticalDomains to the same.
         */
        public JToken[] GetCriticalDomains()
        {
            var criticalDomainsFromDomainsJson = JObject.Parse(File.ReadAllText(PrimeDns.Config.DomainsPath));
            try
            {
                var rawCriticalDomains = criticalDomainsFromDomainsJson["CriticalDomains"].ToArray();
                _criticalDomains = Helper.JsonHelper.GetUniqueCriticalDomains(rawCriticalDomains);
            }
            catch (Exception e)
            {
                _criticalDomains = null;
                PrimeDns.Log._LogWarning("LOOKS LIKE Domains.json IS CORRUPT!!!??",Logger.Logger.CDomainsWatcher,e);
            }
            return _criticalDomains;
        }

        /*
         * UpdateIsDomainCriticalDictionary() clears off the IsDomainCritical Dictionary,
         * And then resets it by reading the Domains.json file afresh.
         */
        private void UpdateIsDomainCriticalDictionary()
        {
            IsDomainCritical.Clear();
            foreach (string domain in _criticalDomains)
            {
                try
                {
                    if (DomainYetToBeAddedToMap[domain])
                    {
                        PrimeDns.Log._LogInformation("DomainYetToBeAddedToMap says this domain needs to be added still... so skipping IsDomainCritical addition!!!??", Logger.Logger.CDomainsWatcher, null);
                    }
                    else
                    {
                        PrimeDns.Log._LogWarning("DomainYetToBeAddedToMap has FALSE ENTRY!!!??", Logger.Logger.CDomainsWatcher, null);
                    }
                }
                catch (KeyNotFoundException)
                {
                    IsDomainCritical.Add(domain, true);
                }              
                
            }
        }

        private static async Task<Tuple<PrimeDnsMapRow, bool>> DoWorkAsync(PrimeDnsMapRow pMapRow, CancellationToken pToken)
        {
            Tuple<PrimeDnsMapRow, bool> result = Tuple.Create(pMapRow, false);
            try
            {
                result = await DnsResolver.DnsResolve(pMapRow, pToken);
            }
            catch (Exception e)
            {
                PrimeDns.Log._LogError("DoWorkAsync in Map Updater caused EXCEPTION!", Logger.Logger.CDnsResolver, e);
            }
            return result;
        }
    }
}
