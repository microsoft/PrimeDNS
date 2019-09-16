/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS.Telemetry
{
    internal class Telemetry
    {
        public static void PushNumberOfCriticalDomains(int pNumberOfCriticalDomains)
        {
            if (!PrimeDns.Config.LogTelemetryEnabled) return;
            if(!PrimeDns.Config.IsTelemetryLite)
                LogConnect.PushNumberOfCriticalDomainsToLog(pNumberOfCriticalDomains);
        }

        public static void PushDnsCallsData(string pDomainName, string pStatus, string pType, int pNewIpAdded, int pOldIpRemoved, string pErrorMessage)
        {
            if (!PrimeDns.Config.LogTelemetryEnabled) return;
            if(!PrimeDns.Config.IsTelemetryLite)
                LogConnect.PushDnsCallsDataToLog(pDomainName, pStatus, pType, pNewIpAdded, pOldIpRemoved, pErrorMessage);
            else if (pStatus == "Failure")
                LogConnect.PushDnsCallsDataToLog(pDomainName, pStatus, pType, pNewIpAdded, pOldIpRemoved, pErrorMessage);
            else if (pStatus == "Success" && pErrorMessage != "Success")
                LogConnect.PushDnsCallsDataToLog(pDomainName, pStatus, pType, pNewIpAdded, pOldIpRemoved, pErrorMessage);
        }

        public static void PushStatusOfThread(string pThreadName, string pStatus)
        {
            if (!PrimeDns.Config.LogTelemetryEnabled) return;
            if(!PrimeDns.Config.IsTelemetryLite)
                LogConnect.PushStatusOfThreadToLog(pThreadName, pStatus);
            else if(pStatus == "Failed")
                LogConnect.PushStatusOfThreadToLog(pThreadName, pStatus);
        }

        public static void PushCpuData(float pCpu)
        {
            if (!PrimeDns.Config.LogTelemetryEnabled) return;
            if(!PrimeDns.Config.IsTelemetryLite)
                LogConnect.PushCpuUtilizationToLog(pCpu);
            else if(pCpu > PrimeDns.Config.PerfCpuThreshold)
                LogConnect.PushCpuUtilizationToLog(pCpu);
        }

        public static void PushRamData(float pRam)
        {
            if (!PrimeDns.Config.LogTelemetryEnabled) return;
            if (!PrimeDns.Config.IsTelemetryLite)
                LogConnect.PushRamUtilizationToLog(pRam);
            else if(pRam > PrimeDns.Config.PerfRamThreshold)
                LogConnect.PushRamUtilizationToLog(pRam);
        }

        public static void PushHostfileWrites()
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushHostfileWritesToLog();
        }
    }
}
