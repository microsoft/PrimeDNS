/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS.Map
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Data.Sqlite;
    using Logger;
    using SQLite;
    using DNS;
    using System.Threading.Tasks;
    using System.Threading;

    internal class MapUpdater
    {
        internal int MapUpdaterFrequencyInSeconds;
        private static string _mapConnectionString;
        private static string _stateConnectionString;

        public MapUpdater()
        {
            MapUpdaterFrequencyInSeconds = PrimeDns.Config.MapUpdaterFrequencyInSeconds;
            _mapConnectionString = PrimeDns.Config.MapConnectionString;
            _stateConnectionString = PrimeDns.Config.StateConnectionString;
            if(!File.Exists(PrimeDns.Config.MapDatabasePath))
            {
                CreateAndInitializePrimeDnsState(0,0,0);
            }
        }

        /*
         * MapConfigUpdater() is called by ConfigChangeHandler()
         */
        public void MapConfigUpdater()
        {
            MapUpdaterFrequencyInSeconds = PrimeDns.Config.MapUpdaterFrequencyInSeconds;
        }

        /*
         * UpdateMap() is straight forward to understand.
         */
        internal static void UpdateMap(DateTimeOffset time)
        {
            PrimeDns.Log._LogInformation("Map Updater Started at Time : " + time.ToString(), Logger.ConstStartUp, null);
            Telemetry.Telemetry.PushStatusOfThread("MapUpdater", "Started");
            if (!SqliteConnect.CheckPrimeDnsState(AppConfig.ConstPrimeDnsMapCreated))
            {
                CreatePrimeDnsMap().Wait();
            }

            var t = new Task[2]
                {
                    UpdatePrimeDnsMap(),
                    Task.Delay(new TimeSpan(0, 0, 300))
                };

            var index = Task.WaitAny(t);
            if (index == 1)
            {
                var source = new CancellationTokenSource();
                PrimeDns.DnsResolverCancellationToken = source.Token;
                source.Cancel();
                t[0].Wait();
            }
            Telemetry.Telemetry.PushStatusOfThread("MapUpdater", "Ended");
            PrimeDns.Log._LogInformation("Map Updater Exited at Time : " + time.ToString(), Logger.ConstStartUp, null);
        }

        /*
         * CreatePrimeDnsMap() creates the table PrimeDNSMap,
         * And also inserts the resolved DNS Data for all the valid domain names from Domains.json into this table.
         */
        internal static async Task CreatePrimeDnsMap()
        {
            var isPrimeDnsMapPresent = SqliteConnect.IsTablePresent(AppConfig.ConstTableNamePrimeDnsMap, _mapConnectionString);
            if (isPrimeDnsMapPresent)
            {
                PrimeDns.Log._LogWarning("PrimeDNSMap Table present in DB, dropping it and creating again.", Logger.ConstPrimeDnsStateIntegrity, null);
                SqliteConnect.DropTable(AppConfig.ConstTableNamePrimeDnsMap, _mapConnectionString);
            }
            CreateTable_PrimeDNSMap();
            MakePrimeDnsMapCreatedTrue();

            var criticalDomains = PrimeDns.DomainsConfig.GetCriticalDomains();
            if (criticalDomains == null)
                return;

            var source = new CancellationTokenSource();
            PrimeDns.DnsResolverCancellationToken = source.Token;

            var tasks = new List<Task<Tuple<PrimeDnsMapRow, bool>>>();
            foreach (string domain in criticalDomains)
            {
                if (DomainsConfig.IsDomainNameValid(domain))
                {
                    var mapRow = new PrimeDnsMapRow(domain);
                    tasks.Add(DoWorkAsync(mapRow, PrimeDns.DnsResolverCancellationToken));
                }
                else
                {
                    PrimeDns.Log._LogWarning("Invalid Domain Name - " + domain + " Found in File!", Logger.ConstDomainsWatcher, null);
                    Telemetry.Telemetry.PushDnsCallsData(domain, "Failure", "InvalidDomain", 0, 0, "INVALID-DOMAIN");
                }

                if (tasks.Count < PrimeDns.Config.ParallelDnsCallsLimit) continue;
                foreach (var (item1, item2) in await Task.WhenAll(tasks))
                {
                    if (item2)
                    {
                        WriteToPrimeDnsMap(item1);
                        //Console.WriteLine("Ending Dns Resolver {0}", task.Item1.HostName);
                    }
                    else
                    {
                        PrimeDns.Log._LogInformation("Failure in adding New Domain to PrimeDNSMap " + item1.HostName, Logger.ConstDomainsWatcher, null);
                        try
                        {
                            if (!PrimeDns.DomainsConfig.DomainYetToBeAddedToMap[item1.HostName])
                                PrimeDns.DomainsConfig.DomainYetToBeAddedToMap[item1.HostName] = true;
                        }
                        catch (KeyNotFoundException)
                        {
                            PrimeDns.DomainsConfig.DomainYetToBeAddedToMap.Add(item1.HostName, true);
                        }
                    }
                }
                tasks.Clear();
            }

            if (tasks.Count > 0)
            {
                foreach (var task in await Task.WhenAll(tasks))
                {
                    if (task.Item2)
                    {
                        WriteToPrimeDnsMap(task.Item1);
                        //Console.WriteLine("Ending Dns Resolver {0}", task.Item1.HostName);
                    }
                    else
                    {
                        PrimeDns.Log._LogInformation("Failure in adding New Domain to PrimeDNSMap " + task.Item1.HostName, Logger.ConstDomainsWatcher, null);
                        try
                        {
                            if (!PrimeDns.DomainsConfig.DomainYetToBeAddedToMap[task.Item1.HostName])
                                PrimeDns.DomainsConfig.DomainYetToBeAddedToMap[task.Item1.HostName] = true;
                        }
                        catch (KeyNotFoundException)
                        {
                            PrimeDns.DomainsConfig.DomainYetToBeAddedToMap.Add(task.Item1.HostName, true);
                        }
                    }
                }
                tasks.Clear();
            }
            
            PrimeDns.Log._LogInformation("WriteToPrimeDNSMap Successful", Logger.ConstSqliteExecuteNonQuery, null);
        }

        /*
         * CreateTable_PrimeDNSMap() creates the table PrimeDNSMap.
         */
        private static void CreateTable_PrimeDNSMap()
        {          
            var createCommand = "Create table " + AppConfig.ConstTableNamePrimeDnsMap +
                                " ( HostName varchar(100), IPAddressList varchar(200), LastUpdatedTime datetime, LastCheckedTime datetime, TimeToLiveInSeconds int )";
            try
            {
                var query = SqliteConnect.ExecuteNonQuery(createCommand, _mapConnectionString);
                PrimeDns.Log._LogInformation("Table PrimeDnsMap created successfully", Logger.ConstSqliteExecuteNonQuery, null);
            }
            catch (Exception error)
            {              
                PrimeDns.Log._LogError("Error occured while creating table PrimeDNSMap.", Logger.ConstSqliteExecuteNonQuery, error);
            }
            
        }

        /*
         * CreateTable_PrimeDNSState() creates the table PrimeDNSState.
         */
        private static void CreateTable_PrimeDNSState()
        {
            var createCommand = "Create table " + AppConfig.ConstTableNamePrimeDnsState +
                                " ( FlagName varchar(100), FlagValue boolean )";
            try
            {
                var query = SqliteConnect.ExecuteNonQuery(createCommand, _stateConnectionString);
                PrimeDns.Log._LogInformation("Table PrimeDNSState created successfully", Logger.ConstSqliteExecuteNonQuery, null);
            }
            catch (Exception error)
            {               
                PrimeDns.Log._LogError("Error occured while creating table PrimeDNSState.", Logger.ConstSqliteExecuteNonQuery, error);
            }
        }

        /*
         * WriteToPrimeDnsMap() inserts a given row data into PrimeDNSMap Table.
         */
        public static void WriteToPrimeDnsMap(PrimeDnsMapRow pMapRowToBeInserted)
        {
            PrimeDns.Semaphore.Wait();
            var insertSql = "Insert into " + AppConfig.ConstTableNamePrimeDnsMap +
                            $" ( HostName, IPAddressList, LastUpdatedTime, LastCheckedTime, TimeToLiveInSeconds) values (\"{pMapRowToBeInserted.HostName}\", \"{pMapRowToBeInserted.GetStringOfIpAddressList()}\", \"{pMapRowToBeInserted.LastUpdatedTime}\", \"{pMapRowToBeInserted.LastCheckedTime}\", {pMapRowToBeInserted.TimeToLiveInSeconds})";
            try
            {
                var query = SqliteConnect.ExecuteNonQuery(insertSql, _mapConnectionString);
                //PrimeDns.logger._LogInformation("Data inserted into PrimeDNSMap table successfully", Logger.CSqliteExecuteNonQuery, null);
            }
            catch (Exception error)
            {                 
                PrimeDns.Log._LogError("Error occured while inserting data into PrimeDNSMap table", Logger.ConstSqliteExecuteNonQuery, error);
            }
            PrimeDns.Semaphore.Release();
        }

        /*
         * MakePrimeDnsMapCreatedTrue() sets the PrimeDNSMapCreated flag to true in PrimeDNSState Table.
         */
        private static void MakePrimeDnsMapCreatedTrue()
        {
            var updateCommand = "UPDATE " + AppConfig.ConstTableNamePrimeDnsState + " SET FlagValue=1" +
                                $" WHERE FlagName=\"{AppConfig.ConstPrimeDnsMapCreated}\"";
            try
            {
                var numberOfRowsUpdated = SqliteConnect.ExecuteNonQuery(updateCommand, _stateConnectionString);
                PrimeDns.Log._LogInformation("PrimeDNSState table updated - # of rows updated - " + numberOfRowsUpdated, Logger.ConstSqliteExecuteNonQuery, null);
            }
            catch (Exception error)
            {               
                PrimeDns.Log._LogError("Error occured while updating PrimeDNSState table on Database", Logger.ConstSqliteExecuteNonQuery, error);
            }
        }

        /*
         * CreateAndInitializePrimeDnsState() does exactly what it says. Initializes all flags in PrimeDNSState to false.
         */
        internal static void CreateAndInitializePrimeDnsState(int pSectionCreatedFlag, int pMapCreatedFlag, int pCriticalDomainsUpdatedFlag)
        {
            CreateTable_PrimeDNSState();
            var insertCommand = "Insert into " + AppConfig.ConstTableNamePrimeDnsState +
                                $" values (\"{AppConfig.ConstPrimeDnsSectionCreated}\", {pSectionCreatedFlag})";
            try
            {
                SqliteConnect.ExecuteNonQuery(insertCommand, _stateConnectionString);
                PrimeDns.Log._LogInformation("Successfully Initialized PrimeDNSSectionCreated as False in the PrimeDNSState", Logger.ConstPrimeDnsStateIntegrity, null);
            }
            catch (Exception error)
            {               
                PrimeDns.Log._LogError("Error occured while Initializing PrimeDNSSectionCreated as False in the PrimeDNSState", Logger.ConstPrimeDnsStateIntegrity, error);
            }

            insertCommand = "Insert into " + AppConfig.ConstTableNamePrimeDnsState +
                            $" values (\"{AppConfig.ConstPrimeDnsMapCreated}\", {pMapCreatedFlag})";
            try
            {
                SqliteConnect.ExecuteNonQuery(insertCommand, _stateConnectionString);
                PrimeDns.Log._LogInformation("Successfully Initialized PrimeDNSMapCreated as False in the PrimeDNSState", Logger.ConstPrimeDnsStateIntegrity, null);
            }
            catch (Exception error)
            {               
                PrimeDns.Log._LogError("Error occured while Initializing PrimeDNSMapCreated as False in the PrimeDNSState", Logger.ConstPrimeDnsStateIntegrity, error);
            }

            insertCommand = "Insert into " + AppConfig.ConstTableNamePrimeDnsState +
                            $" values (\"{AppConfig.ConstPrimeDnsCriticalDomainsUpdated}\", {pCriticalDomainsUpdatedFlag})";
            try
            {
                SqliteConnect.ExecuteNonQuery(insertCommand, _stateConnectionString);
                PrimeDns.Log._LogInformation("Successfully Initialized PrimeDNSCriticalDomainsUpdated as False in the PrimeDNSState", Logger.ConstPrimeDnsStateIntegrity, null);
            }
            catch (Exception error)
            {              
                PrimeDns.Log._LogError("Error occured while Initializing PrimeDNSCriticalDomainsUpdated as False in the PrimeDNSState", Logger.ConstPrimeDnsStateIntegrity, error);

            }
        }

        /*
         * UpdatePrimeDnsMap() goes through all those rows of PrimeDNSMap whose TTL is less than the set threshold
         *  The row gets updated by calling DNS Resolver only if
         *      Host Name is valid and critical, the other cases are logged.
         *  If Host Name found in Map is no more critical, that row is deleted from the table.
         */
        private static async Task UpdatePrimeDnsMap()
        {
            PrimeDns.Log._LogInformation("UpdatePrimeDNSMap Started", Logger.ConstStartUp, null);

            var  selectCommand = "Select * from " + AppConfig.ConstTableNamePrimeDnsMap;
            var tasks = new List<Task<Tuple<PrimeDnsMapRow, bool>>>();
            var hostNamesToBeDeleted = new List<string>();

            using (var connection = new SqliteConnection(_mapConnectionString))
            {
                connection.Open();
                CancellationTokenSource source = new CancellationTokenSource();
                PrimeDns.DnsResolverCancellationToken = source.Token;
                using (var c = new SqliteCommand(selectCommand, connection))
                {
                    using(var query = c.ExecuteReader())
                    {
                        while (query.Read())
                        {
                            DateTime d = query.GetDateTime(3).AddSeconds(query.GetInt32(4) + PrimeDns.Config.TimeToLiveThresholdInSeconds);
                            if (d > DateTime.Now)
                            {
                                continue;
                            }
                            var hostName = query.GetString(0);
                            //Console.WriteLine(hostName);
                            try
                            {
                                var isHostNameCritical = PrimeDns.DomainsConfig.IsDomainCritical[hostName];
                                if (isHostNameCritical && DomainsConfig.IsDomainNameValid(hostName))
                                {
                                    var updatedMapRow = new PrimeDnsMapRow(hostName);
                                    updatedMapRow.GetIpAddressListOfString(query.GetString(1));
                                    updatedMapRow.LastCheckedTime = query.GetDateTime(2);
                                    updatedMapRow.LastUpdatedTime = query.GetDateTime(3);
                                    updatedMapRow.TimeToLiveInSeconds = query.GetInt32(4);

                                    tasks.Add(DoWorkAsync(updatedMapRow, PrimeDns.DnsResolverCancellationToken));
                                }
                                if (!DomainsConfig.IsDomainNameValid(hostName))
                                {
                                    PrimeDns.Log._LogWarning("Invalid Domain Name Found in Map!", Logger.ConstDomainsWatcher, null);
                                    hostNamesToBeDeleted.Add(hostName);
                                }
                                if (!isHostNameCritical)
                                {
                                    PrimeDns.Log._LogInformation("Dictionary having a FALSE entry!!!!? " + hostName, Logger.ConstDomainsWatcher, null);
                                }
                            }
                            catch (KeyNotFoundException)
                            {
                                PrimeDns.Log._LogInformation("Removed the old Domain from PrimeDNSMap " + hostName, Logger.ConstDomainsWatcher, null);
                                hostNamesToBeDeleted.Add(hostName);
                            }
                        }
                   
                        /*
                        if (tasks.Count > PrimeDns.Config.ParallelDnsCallsLimit)
                        {
                            foreach (var task in await Task.WhenAll(tasks))
                            {
                                if (task.Item2)
                                {
                                    UpdatePrimeDnsMapRow(task.Item1);
                                    //Console.WriteLine("Ending Dns Resolver {0}", task.Item1.HostName);
                                }
                            }
                            tasks.Clear();
                        }
                        */
                    }
                }
                connection.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (tasks.Count > 0)
            {
                foreach (var (item1, item2) in await Task.WhenAll(tasks) )
                {
                    if (item2)
                    {
                        UpdatePrimeDnsMapRow(item1);
                        //Console.WriteLine("Ending Dns Resolver {0}", task.Item1.HostName);
                    }
                    else
                    {
                        UpdateLastCheckedTime(item1);
                    }
                }
                tasks.Clear();
            }

            if (hostNamesToBeDeleted.Count > 0)
            {
                foreach (var s in hostNamesToBeDeleted)
                {
                    DeletePrimeDnsMapRow(s);
                }
                hostNamesToBeDeleted.Clear();
            }
            PrimeDns.Log._LogInformation("Updated PrimeDNSMap table successfully", Logger.ConstSqliteExecuteNonQuery, null);

        }

        /*
         * UpdatePrimeDnsMapRow() updates the row containing the mentioned hostName to newly passed values in PrimeDNSMap Table.
         */
        private static void UpdatePrimeDnsMapRow(PrimeDnsMapRow pUpdatedMapRow)
        {
            PrimeDns.Semaphore.Wait();
            var updateCommand = "UPDATE " + AppConfig.ConstTableNamePrimeDnsMap +
                                $" SET IPAddressList=\"{pUpdatedMapRow.GetStringOfIpAddressList()}\", LastUpdatedTime=\"{pUpdatedMapRow.LastUpdatedTime}\", LastCheckedTime=\"{pUpdatedMapRow.LastCheckedTime}\", TimeToLiveInSeconds={pUpdatedMapRow.TimeToLiveInSeconds}" +
                                $" WHERE HostName=\"{pUpdatedMapRow.HostName}\"";
            try
            {
                var query = SqliteConnect.ExecuteNonQuery(updateCommand, _mapConnectionString);
                //PrimeDns.logger._LogInformation("Updated PrimeDNSMap table successfully", Logger.CSqliteExecuteNonQuery, null);
            }
            catch (Exception error)
            {               
                PrimeDns.Log._LogError("Error occured while updating PrimeDNSMap table", Logger.ConstSqliteExecuteNonQuery, error);
            }
            PrimeDns.Semaphore.Release();
        }

        /*
        * UpdatePrimeDnsMapRow() updates the row containing the mentioned hostName to newly passed values in PrimeDNSMap Table.
        */
        private static void UpdateLastCheckedTime(PrimeDnsMapRow pUpdatedMapRow)
        {
            PrimeDns.Semaphore.Wait();
            var updateCommand = "UPDATE " + AppConfig.ConstTableNamePrimeDnsMap +
                                $" SET LastCheckedTime=\"{pUpdatedMapRow.LastCheckedTime}\" " +
                                $" WHERE HostName=\"{pUpdatedMapRow.HostName}\"";
            try
            {
                var query = SqliteConnect.ExecuteNonQuery(updateCommand, _mapConnectionString);
                //PrimeDns.logger._LogInformation("Updated PrimeDNSMap table successfully", Logger.CSqliteExecuteNonQuery, null);
            }
            catch (Exception error)
            {
                PrimeDns.Log._LogError("Error occured while updating PrimeDNSMap table for Last Checked Time", Logger.ConstSqliteExecuteNonQuery, error);
            }
            PrimeDns.Semaphore.Release();
        }

        /*
         * DeletePrimeDnsMapRow() deletes row with pHostName from PrimeDNSMap Table.
         */
        private static void DeletePrimeDnsMapRow(string pHostName)
        {
            PrimeDns.Semaphore.Wait();
            var deleteCommand =
                "DELETE FROM " + AppConfig.ConstTableNamePrimeDnsMap + $" WHERE HostName=\"{pHostName}\"";
            try
            {
                var query = SqliteConnect.ExecuteNonQuery(deleteCommand, _mapConnectionString);
                //PrimeDns.logger._LogInformation("Deleted Row from PrimeDNSMap successfully", Logger.CSqliteExecuteNonQuery, null);
            }
            catch (Exception error)
            {               
                PrimeDns.Log._LogError("Error occured while deleting row from PrimeDNSMap table", Logger.ConstSqliteExecuteNonQuery, error);
            }
            PrimeDns.Semaphore.Release();
        }       

        private static async Task<Tuple<PrimeDnsMapRow,bool>> DoWorkAsync(PrimeDnsMapRow pMapRow, CancellationToken pToken)
        {
            var result = Tuple.Create(pMapRow, false);
            try
            {
                result = await DnsResolver.DnsResolve(pMapRow, pToken);
            }
            catch(Exception e)
            {
                PrimeDns.Log._LogError("DoWorkAsync in Map Updater caused EXCEPTION!", Logger.ConstDnsResolver, e);
            }
            return result;
        }
    }
}
