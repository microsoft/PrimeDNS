namespace PrimeDNS.Telemetry
{
    class Telemetry
    {
        public static void PushNumberOfCriticalDomains(int pNumberOfCriticalDomains)
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushNumberOfCriticalDomainsToLog(pNumberOfCriticalDomains);
        }

        public static void PushDnsCallsData(string pDomainName, string pStatus, string pType, int pNewIPAdded, int pOldIPRemoved, string pErrorMessage)
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushDnsCallsDataToLog(pDomainName, pStatus, pType, pNewIPAdded, pOldIPRemoved, pErrorMessage);
        }

        public static void PushStatusOfThread(string pThreadName, string pStatus)
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushStatusOfThreadToLog(pThreadName, pStatus);
        }

        public static void PushCPUData(float pCpu)
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushCpuUtilizationToLog(pCpu);
        }

        public static void PushRAMData(float pRam)
        {
            if (PrimeDns.Config.LogTelemetryEnabled)
                LogConnect.PushRamUtilizationToLog(pRam);
        }
    }
}
