namespace PrimeDNS.SQLite
{
    using System;
    using Microsoft.Data.Sqlite;
    using Logger;
    using System.Threading;

    internal class SqliteConnect
    {
        /*
         * IsTablePresent() returns true/false depending on whether the table exists or not. 
         */

        public static bool IsTablePresent(string pTableName, string pConnectionString)
        {
            SqliteDataReader query = null;

            using (var Connection = new SqliteConnection(pConnectionString))
            {
                Connection.Open();
                var selectCommand = new SqliteCommand("Select name FROM sqlite_master WHERE type = 'table' AND name = @tableName", Connection);
                selectCommand.Parameters.AddWithValue("@tableName", pTableName);
              
                try
                {
                    query = selectCommand.ExecuteReader();
                    PrimeDns.Log._LogInformation("Table existence queried successfully", Logger.ConstSqliteExecuteReader, null);
                }
                catch (SqliteException error)
                {
                    PrimeDns.Log._LogInformation("Error occured while querying  table existence.", Logger.ConstSqliteExecuteReader, error);
                    PrimeDns.Log._LogError("Error occured while querying  table existence.", Logger.ConstSqliteExecuteReader, error);
                }
                Connection.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            
            if (query == null)
                return false;

            return (query.ToString() == pTableName);
        }

        /*
         * DropTable() drops all entries of the table.
         */
        public static void DropTable(string pTableName, string pConnectionString)
        {
            using (var Connection = new SqliteConnection(pConnectionString))
            {
                Connection.Open();
                var dropCommand = new SqliteCommand("Drop table if exists " + pTableName, Connection);
                try
                {
                    dropCommand.ExecuteNonQuery();
                    PrimeDns.Log._LogInformation("Table dropped successfully", Logger.ConstSqliteExecuteNonQuery, null);
                }
                catch (SqliteException error)
                {
                    PrimeDns.Log._LogInformation("Error occured while dropping table.", Logger.ConstSqliteExecuteNonQuery, error);
                    PrimeDns.Log._LogError("Error occured while dropping table.", Logger.ConstSqliteExecuteNonQuery, error);
                }
                Connection.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }          
        }

        /*
         * DeleteTable() - deletes the table.
         */
        public static void DeleteTable(string pTableName, string pConnectionString)
        {
            using (var Connection = new SqliteConnection(pConnectionString))
            {
                Connection.Open();
                var dropCommand = new SqliteCommand("Delete from " + pTableName, Connection);
                try
                {
                    dropCommand.ExecuteNonQuery();
                    PrimeDns.Log._LogInformation("Table entries deleted successfully", Logger.ConstSqliteExecuteNonQuery, null);
                }
                catch (SqliteException error)
                {
                    PrimeDns.Log._LogInformation("Error occured while deleting entries from table.", Logger.ConstSqliteExecuteNonQuery, error);
                    PrimeDns.Log._LogError("Error occured while deleting entries from table.", Logger.ConstSqliteExecuteNonQuery, error);
                }
                Connection.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }         
        }

        public static bool CheckPrimeDNSState(string pState)
        {
            var flagToReturn = false;
            var selectCommand = String.Format("select * from " + AppConfig.CTableNamePrimeDnsState);
            try
            {
                using(var Connection = new SqliteConnection(PrimeDns.Config.StateConnectionString))
                {
                    Connection.Open();

                    using (var c = new SqliteCommand(selectCommand, Connection))
                    {
                        using (var query = c.ExecuteReader())
                        {
                            while (query.Read())
                            {
                                if (query.GetString(0) == pState)
                                {
                                    flagToReturn = query.GetBoolean(1);
                                }
                            }
                        }
                    }
                    
                }
                
                PrimeDns.Log._LogInformation("PrimeDNS State value successfully taken from PrimeDNSState - " + pState, Logger.ConstSqliteExecuteReader, null);
            }
            catch (Exception error)
            {
                PrimeDns.Log._LogError("Error in accessing PrimeDNSState Table from Database - " + pState, Logger.ConstSqliteExecuteReader, error);
            }
            return flagToReturn;
        }

        public static int ExecuteNonQuery(string pCommand, string pConnectionString)
        {
            int query = 0;
            bool flag = false;
            //PrimeDns.semaphore.Wait();
            using (var Connection = new SqliteConnection(pConnectionString))
            {
                Connection.Open();
                try
                {
                    using(var c = new SqliteCommand(pCommand, Connection))
                    {                        
                        query = c.ExecuteNonQuery();                       
                    }
                    
                }
                catch (SqliteException e)
                {
                    //PrimeDns.semaphore.Release();
                    if (e.Message.Contains("database is locked"))
                    {
                        flag = true;
                    }
                    PrimeDns.Log._LogError("SQlite Execute Non Query Error\n" + pCommand + "\n" + pConnectionString + "****\n", Logger.ConstSqliteExecuteNonQuery, e);
                }
                Connection.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (flag)
            {
                //CleanUp.Clean();
                //CleanUp.CreateMap();
                //Thread.Sleep(100);
            }
            //PrimeDns.semaphore.Release();
            return query;
        }
    }
}
