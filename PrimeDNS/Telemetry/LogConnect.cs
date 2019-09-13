/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS.Telemetry
{
    using Microsoft.Extensions.Logging;

    internal class LogConnect
    {
        private const int CMetricNumberOfCriticalDomains = 1;
        private const int CMetricDnsCalls = 2;
        private const int CThreadStatus = 3;
        private const int CPerf = 4;
        private const int CNumberOfHostfileWrites = 5;

        private static readonly ILoggerFactory LoggerFactory = new LoggerFactory()
                .AddFile("Telemetry_Logs/PrimeDns-Telemetry-{Date}.txt");
        private static readonly ILogger Logger = LoggerFactory.CreateLogger<LogConnect>();

        public static void PushNumberOfCriticalDomainsToLog(int pNumberOfCriticalDomains)
        {
            Logger.LogInformation("|1|NumberOfCriticalDomains|" + pNumberOfCriticalDomains, CMetricNumberOfCriticalDomains, null);
        }

        public static void PushDnsCallsDataToLog(string pDomainName, string pStatus, string pType, int pNewIpAdded, int pOldIpRemoved, string pErrorMessage)
        {
            Logger.LogInformation("|7|DnsCalls|1|" + pType + "|" + pDomainName + "|" + pStatus + "|" + pNewIpAdded + "|" + pOldIpRemoved + "|" + pErrorMessage + "|", CMetricDnsCalls, null);
        }

        public static void PushStatusOfThreadToLog(string pThreadName, string pStatus)
        {
            Logger.LogInformation("|3|ThreadStatus|1|" + pThreadName + "|" + pStatus, CThreadStatus, null);
        }

        public static void PushCpuUtilizationToLog(float pCpuUtilization)
        {
            Logger.LogInformation("|1|CPU|" + pCpuUtilization, CPerf, null);
        }

        public static void PushRamUtilizationToLog(float pRamUtilization)
        {
            Logger.LogInformation("|1|RAM|" + pRamUtilization, CPerf, null);
        }

        public static void PushHostfileWritesToLog()
        {
            Logger.LogInformation("|1|Hostfile Writes|1|", CNumberOfHostfileWrites, null);
        }
    }
}
