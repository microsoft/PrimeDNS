using System;

namespace PrimeDNS.HostFile
{
    using System.IO;
    using System.Text;
    using Helper;
    using Logger;
    internal class IntegrityChecker
    {
        /*
         * CheckPrimeDnsSectionIntegrity() checks if the Begin and End markers of PrimeDNS are in order in hostfile.
         */
        public static bool CheckPrimeDnsSectionIntegrity(string pHostFilePath)
        {
            using (var sr = new StreamReader(pHostFilePath, Encoding.UTF8))
            {
                var contents = sr.ReadToEnd();
                var startPosition = contents.IndexOf(PrimeDns.Config.PrimeDnsSectionBeginString, StringComparison.Ordinal);
                var endPosition = contents.IndexOf(PrimeDns.Config.PrimeDnsSectionEndString, StringComparison.Ordinal);

                if (startPosition > endPosition)
                {
                    PrimeDns.Log._LogWarning("###---PrimeDNS-BEGIN-SECTION appearing before ###---PrimeDNS-END-SECTION", Logger.CHostFileIntegrity, null);
                    return false;
                }
                else if (!(startPosition >= 0 && endPosition >= 0))
                {
                    PrimeDns.Log._LogWarning("###---PrimeDNS-BEGIN-SECTION or ###---PrimeDNS-END-SECTION missing!", Logger.CHostFileIntegrity, null);
                    return false;
                }
                else
                    return true;
            }
        }

        /*
         * CheckPrimeDnsSectionPresence checks if either Begin or End PrimeDNS markers are present in the file.
         * If present, this is logged as unexpected and these markers are removed from file.
         * Note: This function is called after it has been checked that flag PrimeDNSSectionCreated in PrimeDnsState is false. 
         */

        public static void CheckPrimeDnsSectionPresence()
        {
            var isPrimeDnsSectionPresent = false;
            using (var sr = new StreamReader(PrimeDns.Config.HostFilePath, Encoding.UTF8))
            {
                var contents = sr.ReadToEnd();
                if (contents.Contains(PrimeDns.Config.PrimeDnsSectionBeginString))
                {
                    PrimeDns.Log._LogWarning("###---PrimeDNS-BEGIN-SECTION present in file, inspite of PrimeDNSState stating otherwise!", Logger.CPrimeDnsStateIntegrity, null);
                    isPrimeDnsSectionPresent = true;
                }
                if (contents.Contains(PrimeDns.Config.PrimeDnsSectionEndString))
                {
                    PrimeDns.Log._LogWarning("###---PrimeDNS-END-SECTION present in file, inspite of PrimeDNSState stating otherwise!", Logger.CPrimeDnsStateIntegrity, null);
                    isPrimeDnsSectionPresent = true;
                }
            }

            if (!isPrimeDnsSectionPresent)
                return;
            FileHelper.RemoveLineFromFile(PrimeDns.Config.HostFilePath, PrimeDns.Config.PrimeDnsSectionBeginString);
            FileHelper.RemoveLineFromFile(PrimeDns.Config.HostFilePath, PrimeDns.Config.PrimeDnsSectionEndString);
        }
    }
}
