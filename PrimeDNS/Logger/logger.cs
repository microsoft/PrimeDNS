namespace PrimeDNS.Logger
{
    using Microsoft.Extensions.Logging;
    using System;
    internal class Logger
    {
        private static ILoggerFactory _loggerFactory;
        private static ILogger _logger;

        public const int CTtlUpdater = 3;
        public const int CDnsResolver = 6;
        public const int CconfigWatcher = 9;
        public const int CDomainsWatcher = 18;
        public const int CStartUp = 1008;

        public const int CSqliteExecuteReader = 108;
        public const int CSqliteExecuteNonQuery = 109;
        public const int CHostFileIntegrity = 110;
        public const int CTaskException = 111;
        public const int CPrimeDnsStateIntegrity = 112;

        public const int CMdmTelemetry = 113;

        public Logger()
        {
            if (_loggerFactory != null)
                return;
            _loggerFactory = new LoggerFactory()
                .AddConsole()
                .AddDebug()
                .AddFile("Logs/PrimeDNS-{Date}.txt");
            _logger = _loggerFactory.CreateLogger<PrimeDns>();

        }
        public void _LogError(string pLogMessage, int pEvent, Exception pError)
        {
            if(PrimeDns.Config.IsErrorLogEnabled)
                _logger.LogError(pEvent, pError, pLogMessage);
        }
        public void _LogInformation(string pLogMessage, int pEvent, Exception pError)
        {
            if(PrimeDns.Config.IsInformationLogEnabled)
                _logger.LogInformation(pEvent, pError, pLogMessage);
        }
        public void _LogWarning(string pLogMessage, int pEvent, Exception pError)
        {   
            if(PrimeDns.Config.IsWarningLogEnabled)
                _logger.LogWarning(pEvent, pError, pLogMessage);
        }
    }
}
