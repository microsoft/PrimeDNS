/* -----------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 * ----------------------------------------------------------------------- */

namespace PrimeDNS.Logger
{
    using Microsoft.Extensions.Logging;
    using System;
    internal class Logger
    {
        private static ILoggerFactory _loggerFactory;
        private static ILogger _logger;

        public const int ConstTtlUpdater = 3;
        public const int ConstDnsResolver = 6;
        public const int ConstConfigWatcher = 9;
        public const int ConstDomainsWatcher = 18;
        public const int ConstStartUp = 1008;

        public const int ConstSqliteExecuteReader = 108;
        public const int ConstSqliteExecuteNonQuery = 109;
        public const int ConstHostFileIntegrity = 110;
        public const int ConstTaskException = 111;
        public const int ConstPrimeDnsStateIntegrity = 112;

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
