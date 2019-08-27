namespace PrimeDNS
{
    using PrimeDNS.Map;
    using System.IO;

    internal class CleanUp
    {
        public static void Clean()
        {
            //PrimeDns.semaphore.Wait();

            PrimeDns.Log._LogInformation("CLEANING UP Started", Logger.Logger.ConstStartUp, null);

            if (File.Exists(PrimeDns.Config.MapDatabasePath))
                File.Delete(PrimeDns.Config.MapDatabasePath);

            if (File.Exists(PrimeDns.Config.StateDatabasePath))
                File.Delete(PrimeDns.Config.StateDatabasePath);

            if (File.Exists(PrimeDns.Config.MapDatabaseJournalPath))
                File.Delete(PrimeDns.Config.MapDatabaseJournalPath);

            PrimeDns.HostFileUpdater.RemoveOldPrimeDnsSectionEntries(PrimeDns.Config.HostFilePath);
            Helper.FileHelper.RemoveLineFromFile(PrimeDns.Config.HostFilePath, PrimeDns.Config.PrimeDnsSectionBeginString);
            Helper.FileHelper.RemoveLineFromFile(PrimeDns.Config.HostFilePath, PrimeDns.Config.PrimeDnsSectionEndString);
            MapUpdater.CreateAndInitializePrimeDnsState(0, 0, 0);

            //PrimeDns.semaphore.Release();

            PrimeDns.Log._LogInformation("CLEANING UP Ended", Logger.Logger.ConstStartUp, null);
        }

        public static void CreateMap()
        {
            MapUpdater.CreatePrimeDnsMap().Wait();
        }
    }
}