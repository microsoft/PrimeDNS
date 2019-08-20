namespace PrimeDNS.Telemetry
{
    using Microsoft.Extensions.Logging;

    internal class LogConnect
    {
        private const int CMetric_NumberOfCriticalDomains = 1;
        private const int CMetric_DnsCalls = 2;
        private const int CThread_Status = 3;
        private const int CPerf = 4;

        private static ILoggerFactory _loggerFactory = new LoggerFactory()
                .AddFile("Telemetry_Logs/PrimeDns-Telemetry-{Date}.txt");
        private static ILogger _logger = _loggerFactory.CreateLogger<LogConnect>();

        public static void PushNumberOfCriticalDomainsToLog(int pNumberOfCriticalDomains)
        {
            _logger.LogInformation("|1|NumberOfCriticalDomains|" + pNumberOfCriticalDomains, CMetric_NumberOfCriticalDomains, null);
        }

        public static void PushDnsCallsDataToLog(string pDomainName, string pStatus, string pType, int pNewIPAdded, int pOldIPRemoved, string pErrorMessage)
        {
            _logger.LogInformation("|7|DnsCalls|1|" + pType + "|" + pDomainName + "|" + pStatus + "|" + pNewIPAdded + "|" + pOldIPRemoved + "|" + pErrorMessage + "|", CMetric_DnsCalls, null);
        }

        public static void PushStatusOfThreadToLog(string pThreadName, string pStatus)
        {
            _logger.LogInformation("|3|ThreadStatus|1|" + pThreadName + "|" + pStatus, CThread_Status, null);
        }

        public static void PushCpuUtilizationToLog(float pCpuUtilization)
        {
            _logger.LogInformation("|1|CPU|" + pCpuUtilization, CPerf, null);
        }

        public static void PushRamUtilizationToLog(float pRamUtilization)
        {
            _logger.LogInformation("|1|RAM|" + pRamUtilization, CPerf, null);
        }
    }
}
