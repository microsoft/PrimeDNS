/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS.DNS
{
    using Microsoft.Data.Sqlite;
    using Map;
    using SQLite;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class TimeToLiveUpdater
    {
        internal static int TimeToLiveUpdaterFrequencyInSeconds;
        private static string _mapConnectionString;

        public TimeToLiveUpdater()
        {
            TimeToLiveUpdaterFrequencyInSeconds = PrimeDns.Config.TimeToLiveUpdaterFrequencyInSeconds;
            _mapConnectionString = PrimeDns.Config.MapConnectionString;
        }

        internal async Task UpdateTtl(DateTimeOffset time)
        {
            PrimeDns.Log._LogInformation("TTL Updater Started at Time : " + time.ToString(), Logger.Logger.ConstTtlUpdater, null);
            await UpdateTimeToLive();
            PrimeDns.Log._LogInformation("TTL Updater Exited at Time : " + time.ToString(), Logger.Logger.ConstTtlUpdater, null);
        }

        internal async Task UpdateTimeToLive()
        {
            var selectCommand = string.Format("Select * from " + AppConfig.CTableNamePrimeDnsMap);
            var tasks = new List<Task<Tuple<PrimeDnsMapRow, bool>>>();
            var ttlUpdateList = new List<PrimeDnsMapRow>();

            using (var connection = new SqliteConnection(_mapConnectionString))
            {
                connection.Open();
                using(var c = new SqliteCommand(selectCommand, connection))
                {
                    using (var query = c.ExecuteReader())
                    {
                        while (query.Read())
                        {
                            var hostName = query.GetString(0);
                            var timeToLive = query.GetInt32(4);

                            if (DomainsConfig.IsDomainNameValid(hostName))
                            {
                                var updatedMapRow = new PrimeDnsMapRow(hostName);
                                tasks.Add(DoWorkAsync(updatedMapRow));
                            }

                            
                            
                            if (tasks.Count > PrimeDns.Config.ParallelTtlCallsLimit)
                            {
                                foreach (var (item1, item2) in await Task.WhenAll(tasks))
                                {
                                    if (item2)
                                    {
                                        ttlUpdateList.Add(item1);
                                        //Console.WriteLine("Ending Ttl Resolver {0}", task.Item1.HostName);
                                    }
                                    else
                                    {
                                        PrimeDns.TtlUpdaterErrorCount++;
                                    }
                                }
                                tasks.Clear();
                            }


                            if (PrimeDns.TtlUpdaterErrorCount >= PrimeDns.Config.TtlUpdaterErrorLimit)
                            {
                                PrimeDns.Log._LogWarning("TOO MANY ERRORS IN TTL UPDATER!! Breaking..", Logger.Logger.ConstTtlUpdater, null);
                                break;
                            }
                        }
                    }                      
                }
                connection.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (tasks.Count > 0)
            {
                foreach (var (item1, item2) in await Task.WhenAll(tasks))
                {
                    if (item2)
                    {
                        ttlUpdateList.Add(item1);
                        //Console.WriteLine("Ending Ttl Resolver {0}", task.Item1.HostName);
                    }
                }
                tasks.Clear();
            }

            if(ttlUpdateList.Count > 0)
            {
                foreach(var mapElement in ttlUpdateList)
                {
                    UpdatePrimeDnsMapRow(mapElement.HostName, mapElement.TimeToLiveInSeconds);
                }
            }

        }

        private static void UpdatePrimeDnsMapRow(string pDomain, int pUpdatedTtl)
        {
            var updateCommand = "UPDATE " + AppConfig.CTableNamePrimeDnsMap +
                                $" SET TimeToLiveInSeconds={pUpdatedTtl}" + $" WHERE HostName={pDomain}";
            try
            {
                SqliteConnect.ExecuteNonQuery(updateCommand, _mapConnectionString);
                //PrimeDns.logger._LogInformation("Updated PrimeDNSMap table successfully", Logger.Logger.CSqliteExecuteNonQuery, null);
            }
            catch (Exception error)
            {              
                PrimeDns.Log._LogError("Error occured while updating PrimeDNSMap table for TTL Updater", Logger.Logger.ConstSqliteExecuteNonQuery, error);
            }
        }

        private static async Task<Tuple<PrimeDnsMapRow, bool>> DoWorkAsync(PrimeDnsMapRow pMapRow)
        {
            var result = Tuple.Create(pMapRow, false);
            try
            {
                result = await TimeToLiveResolver.TtlResolve(pMapRow);
            }
            catch (Exception e)
            {
                PrimeDns.Log._LogError("DoWorkAsync in TTL Updater caused EXCEPTION!", Logger.Logger.ConstTtlUpdater, e);
            }
            return result;
        }
    }
}
