namespace PrimeDNS.Helper
{
    using System;
    using System.IO;
    using System.Text;

    internal class FileHelper
    {
        /*
         * As the function name suggests, InsertIntoFile function inserts Data at the given line number of given file. 
         */
        public static void InsertIntoFile(string pPath, string pData, int pLineNumber)
        {
            var tempFile = Path.GetTempFileName();
            var lines = 0;

            using(var sw = new StreamWriter(tempFile))
            {
                using (var f = File.Open(pPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var sr = new StreamReader(f, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (lines == (pLineNumber - 1))
                                sw.WriteLine(pData);
                            sw.WriteLine(line);
                            lines++;
                        }
                    }
                    f.Close();
                }
            }
            int tries = 0;
            bool flag = false;
            while (tries < 3 && !flag)
            {
                try
                {
                    //File.Replace(tempFile, pPath, pPath + ".bak", true);
                    File.Copy(tempFile, pPath, true);
                    File.Delete(tempFile);
                    flag = true;
                }
                catch (AggregateException ae)
                {
                    PrimeDns.Log._LogError("Exception occured while inserting into Hostfile - ", Logger.Logger.CHostFileIntegrity, ae);
                    tries++;
                }
            }            

        }

        /*
         * RemoveLineFromFile function removes given line(string) from given file.
         * Note: All occurances of this line (string) will be removed from the file. 
         */
        public static void RemoveLineFromFile(string pPath, string pLineToRemove)
        {
            var tempFile = Path.GetTempFileName();

            using (var sr = new StreamReader(pPath))
            using (var sw = new StreamWriter(tempFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == pLineToRemove)
                        continue;
                    sw.WriteLine(line);
                }
            }
            //File.Replace(tempFile, pPath, pPath + ".bak", true);
            File.Copy(tempFile, pPath, true);
            File.Delete(tempFile);
        }
    }
}
