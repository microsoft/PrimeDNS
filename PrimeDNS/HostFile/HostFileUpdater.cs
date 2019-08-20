namespace PrimeDNS.HostFile
{
    using System;
    using System.IO;
    using System.Text;
    using Logger;
    using Helper;  
    using SQLite;
    using Microsoft.Data.Sqlite;

    internal class HostFileUpdater
    {
        internal int PrimeDnsBeginLine;
        internal int HostFileUpdaterFrequencyInSeconds;
        private static string mapConnectionString;
        private static string stateConnectionString;
        private static StringBuilder stringBuilder;

        public HostFileUpdater()
        {
            HostFileUpdaterFrequencyInSeconds = PrimeDns.Config.HostFileUpdaterFrequencyInSeconds;
            mapConnectionString = PrimeDns.Config.MapConnectionString;
            stateConnectionString = PrimeDns.Config.StateConnectionString;
        }

        /*
         * HostFileConfigUpdater() is called by ConfigChangeHandler()
         */
        internal void HostFileConfigUpdater()
        {
            HostFileUpdaterFrequencyInSeconds = PrimeDns.Config.HostFileUpdaterFrequencyInSeconds;
        }

        /*
         * UpdateHostfile() - calls various other functions that have comments on them, so should be understandable.
         */
        internal void UpdateHostfile(DateTimeOffset time)
        {
            
            PrimeDns.Log._LogInformation("Host File Updater Started at Time : " + time.ToString(), Logger.CStartUp, null);
            Telemetry.Telemetry.PushStatusOfThread("HostFileUpdater", "Started");
            if (!SqliteConnect.CheckPrimeDNSState(AppConfig.CPrimeDnsSectionCreated))
            {
                CreatePrimeDnsSection();
                MakePrimeDnsSectionCreatedTrue();
            }
            var isPrimeDnsSectionOkay = IntegrityChecker.CheckPrimeDnsSectionIntegrity(PrimeDns.Config.HostFilePath);
            if (isPrimeDnsSectionOkay)
            {                
                try
                {
                    var newPrimeDnsSectionEntries = GetPrimeDnsSectionEntries();
                    var hostfilePath = PrimeDns.Config.HostFilePath;
                    RemoveOldPrimeDnsSectionEntries(hostfilePath);
                    FindPrimeDnsSectionBegin(hostfilePath);
                    if (PrimeDnsBeginLine >= 0)
                        FileHelper.InsertIntoFile(hostfilePath, newPrimeDnsSectionEntries, PrimeDnsBeginLine + 1);
                }
                catch(IOException ioe)
                {
                    PrimeDns.Log._LogError("Aggregate Exception occured while updating Hostfile - ", Logger.CHostFileIntegrity, ioe);
                    Telemetry.Telemetry.PushStatusOfThread("HostFileUpdater", "Failed");
                }                     
            }
            else
            {
                FileHelper.RemoveLineFromFile(PrimeDns.Config.HostFilePath, PrimeDns.Config.PrimeDnsSectionBeginString);
                FileHelper.RemoveLineFromFile(PrimeDns.Config.HostFilePath, PrimeDns.Config.PrimeDnsSectionEndString);
                CreatePrimeDnsSection();
                PrimeDns.Log._LogWarning("CheckPrimeDnsSectionIntegrity FAILED!!, Continuing..",Logger.CHostFileIntegrity,null);
            }
            Telemetry.Telemetry.PushStatusOfThread("HostFileUpdater", "Ended");
            PrimeDns.Log._LogInformation("Host File Updater Ended at Time : " + time.ToString(), Logger.CStartUp, null);

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /*
         * RemoveOldPrimeDnsSectionEntries() does exactly what it's name suggests.
         */
        internal void RemoveOldPrimeDnsSectionEntries(string pFilePath)
        {
            var tempFile = Path.GetTempFileName();

            using (var f = File.Open(pFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var sr = new StreamReader(f))
                {
                    using (var sw = new StreamWriter(tempFile))
                    {
                        string line;

                        while ((line = sr.ReadLine()) != null)
                        {
                            sw.WriteLine(line);
                            if (line == PrimeDns.Config.PrimeDnsSectionBeginString)
                                break;
                        }
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line != PrimeDns.Config.PrimeDnsSectionEndString)
                                continue;
                            sw.WriteLine(line);
                            break;

                        }
                        while ((line = sr.ReadLine()) != null)
                        {
                            sw.WriteLine(line);
                        }
                    }
                }
                f.Close();
            }
            
            int tries = 0;
            bool flag = false;
            while (tries < 3 && !flag)
            {
                try
                {
                    //File.Replace(tempFile, pFilePath, pFilePath + ".bak", true);
                    File.Copy(tempFile, pFilePath, true);
                    File.Delete(tempFile);
                    flag = true;
                }
                catch (AggregateException ae)
                {
                    PrimeDns.Log._LogError("Exception occured while inserting into Hostfile - ", Logger.CHostFileIntegrity, ae);
                    tries++;
                }
            }
        }

        /*
         * FindPrimeDnsSectionBegin() finds the line number of the PrimeDns-Begin-String in the file.
         */
        internal void FindPrimeDnsSectionBegin(string pFilePath)
        {
            PrimeDnsBeginLine = 0;
            int tries = 0;
            bool flag = false;
            while (tries < 3 && !flag)
            {
                try
                {
                    using (var f = File.Open(pFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var sr = new StreamReader(f, Encoding.UTF8))
                        {
                            var line = sr.ReadLine();
                            while (line != PrimeDns.Config.PrimeDnsSectionBeginString)
                            {
                                PrimeDnsBeginLine++;
                                line = sr.ReadLine();
                            }
                            PrimeDnsBeginLine++;
                        }
                        flag = true;
                        f.Close();
                    }
                }
                catch (AggregateException ae)
                {
                    PrimeDns.Log._LogError("Exception occured while Finding PrimeDns Section Begin String in File - ", Logger.CHostFileIntegrity, ae);
                    tries++;
                }
            }

            if (tries >= 3)
                PrimeDnsBeginLine = -1;

        }

        /*
         * CreatePrimeDnsSection(), creates the PrimeDns section in the file.
         */
        private static void CreatePrimeDnsSection()
        {
            IntegrityChecker.CheckPrimeDnsSectionPresence();
            using (var sw = File.AppendText(PrimeDns.Config.HostFilePath))
            {
                sw.WriteLine();
                sw.WriteLine(PrimeDns.Config.PrimeDnsSectionBeginString);
                sw.WriteLine(PrimeDns.Config.PrimeDnsSectionEndString);
            }
        }

        /*
         * MakePrimeDnsSectionCreatedTrue() sets the PrimeDNSSectionCreated Flag in PrimeDNSState Table to True.
         */
        private static void MakePrimeDnsSectionCreatedTrue()
        {
            var updateCommand = String.Format("UPDATE " + AppConfig.CTableNamePrimeDnsState + " SET FlagValue=1" +
                    " WHERE FlagName=\"{0}\"", AppConfig.CPrimeDnsSectionCreated);         
            try
            {
                var numberOfRowsUpdated = SqliteConnect.ExecuteNonQuery(updateCommand, stateConnectionString);
                PrimeDns.Log._LogInformation("PrimeDNSState table updated - # of rows updated - " + numberOfRowsUpdated, Logger.CSqliteExecuteNonQuery, null);
            }
            catch (Exception error)
            {               
                PrimeDns.Log._LogError("Error occured while updating PrimeDNSState table on Database", Logger.CSqliteExecuteNonQuery, error);
            }
        }

        /*
         * GetPrimeDnsSectionEntries() reads PrimeDNSMap Table,
         * and returns all ipAddress-hostName pairs that have TTL above the set threshold.
         */
        private static string GetPrimeDnsSectionEntries()
        {
            PrimeDns.semaphore.Wait();
            string entries;
            stringBuilder =  new StringBuilder("");
            var selectCommand = String.Format("Select * from " + AppConfig.CTableNamePrimeDnsMap +
                    " WHERE TimeToLiveInSeconds > {0}", PrimeDns.Config.TimeToLiveThresholdInSeconds);
            try
            {
                using (var Connection = new SqliteConnection(mapConnectionString))
                {
                    Connection.Open();

                    using (var c = new SqliteCommand(selectCommand, Connection))
                    {
                        using (var query = c.ExecuteReader())
                        {
                            while (query.Read())
                            {
                                var ipAddresses = query.GetString(1).Split('#');
                                var hostName = query.GetString(0);
                                foreach (var ipAddress in ipAddresses)
                                {
                                    if (IpHelper.IsIpAddressValid(ipAddress))
                                    {
                                        entries = (ipAddress + "\t" + hostName + "\n");
                                        stringBuilder.Append(entries);
                                    }
                                }
                            }
                        }
                    }
                }
                PrimeDns.Log._LogInformation("Data pulled from PrimeDNSMap table successfully", Logger.CSqliteExecuteReader, null);
            }
            catch (Exception error)
            {
                PrimeDns.Log._LogError("Error occured while pulling data from PrimeDNSMap table", Logger.CSqliteExecuteNonQuery, error);
            }
            entries = stringBuilder.ToString();
            stringBuilder.Clear();
            PrimeDns.semaphore.Release();
            return entries;
        }

    }
}
