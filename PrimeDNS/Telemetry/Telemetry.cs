/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

using System.Net.NetworkInformation;

namespace PrimeDNS.Telemetry
{
    internal class Telemetry
    {
        public static void PushNumberOfCriticalDomains(int pNumberOfCriticalDomains)
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushNumberOfCriticalDomainsToLog(pNumberOfCriticalDomains);
        }

        public static void PushDnsCallsData(string pDomainName, string pStatus, string pType, int pNewIpAdded, int pOldIpRemoved, string pErrorMessage)
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushDnsCallsDataToLog(pDomainName, pStatus, pType, pNewIpAdded, pOldIpRemoved, pErrorMessage);
        }

        public static void PushStatusOfThread(string pThreadName, string pStatus)
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushStatusOfThreadToLog(pThreadName, pStatus);
        }

        public static void PushCpuData(float pCpu)
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushCpuUtilizationToLog(pCpu);
        }

        public static void PushRamData(float pRam)
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushRamUtilizationToLog(pRam);
        }

        public static void PushHostfileWrites()
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushHostfileWritesToLog();
        }
    }
}
