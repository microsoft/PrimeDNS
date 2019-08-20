namespace PrimeDNS.DNS
{
    using Microsoft.Data.Sqlite;
    using PrimeDNS.Map;
    using PrimeDNS.SQLite;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class TimeToLiveUpdater
    {
        internal static int timeToLiveUpdaterFrequencyInSeconds;
        private static string mapConnectionString;

        public TimeToLiveUpdater()
        {
            timeToLiveUpdaterFrequencyInSeconds = PrimeDns.Config.TimeToLiveUpdaterFrequencyInSeconds;
            mapConnectionString = PrimeDns.Config.MapConnectionString;
        }

        internal async Task UpdateTtl(DateTimeOffset time)
        {
            PrimeDns.Log._LogInformation("TTL Updater Started at Time : " + time.ToString(), Logger.Logger.CTtlUpdater, null);
            await UpdateTimeToLive();
            PrimeDns.Log._LogInformation("TTL Updater Exited at Time : " + time.ToString(), Logger.Logger.CTtlUpdater, null);
        }

        internal async Task UpdateTimeToLive()
        {
            var selectCommand = String.Format("Select * from " + AppConfig.CTableNamePrimeDnsMap);
            var tasks = new List<Task<Tuple<PrimeDnsMapRow, bool>>>();
            var TtlUpdationList = new List<PrimeDnsMapRow>();

            using (var Connection = new SqliteConnection(mapConnectionString))
            {
                Connection.Open();
                using(var c = new SqliteCommand(selectCommand, Connection))
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
                                foreach (var task in await Task.WhenAll(tasks))
                                {
                                    if (task.Item2)
                                    {
                                        TtlUpdationList.Add(task.Item1);
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
                                PrimeDns.Log._LogWarning("TOO MANY ERRORS IN TTL UPDATER!! Breaking..", Logger.Logger.CTtlUpdater, null);
                                break;
                            }
                        }
                    }                      
                }
                Connection.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (tasks.Count > 0)
            {
                foreach (var task in await Task.WhenAll(tasks))
                {
                    if (task.Item2)
                    {
                        TtlUpdationList.Add(task.Item1);
                        //Console.WriteLine("Ending Ttl Resolver {0}", task.Item1.HostName);
                    }
                }
                tasks.Clear();
            }

            if(TtlUpdationList.Count > 0)
            {
                foreach(var mapElement in TtlUpdationList)
                {
                    UpdatePrimeDnsMapRow(mapElement.HostName, mapElement.TimeToLiveInSeconds);
                }
            }

        }

        private void UpdatePrimeDnsMapRow(string pDomain, int pUpdatedTtl)
        {
            var updateCommand = String.Format("UPDATE " + AppConfig.CTableNamePrimeDnsMap + " SET TimeToLiveInSeconds={0}" +
                            " WHERE HostName={1}", pUpdatedTtl, pDomain);
            try
            {
                SqliteConnect.ExecuteNonQuery(updateCommand, mapConnectionString);
                //PrimeDns.logger._LogInformation("Updated PrimeDNSMap table successfully", Logger.Logger.CSqliteExecuteNonQuery, null);
            }
            catch (Exception error)
            {              
                PrimeDns.Log._LogError("Error occured while updating PrimeDNSMap table for TTL Updater", Logger.Logger.CSqliteExecuteNonQuery, error);
            }
        }

        private static async Task<Tuple<PrimeDnsMapRow, bool>> DoWorkAsync(PrimeDnsMapRow pMapRow)
        {
            Tuple<PrimeDnsMapRow, bool> result = Tuple.Create(pMapRow, false);
            try
            {
                result = await TimeToLiveResolver.TtlResolve(pMapRow);
            }
            catch (Exception e)
            {
                PrimeDns.Log._LogError("DoWorkAsync in TTL Updater caused EXCEPTION!", Logger.Logger.CTtlUpdater, e);
            }
            return result;
        }
    }
}
