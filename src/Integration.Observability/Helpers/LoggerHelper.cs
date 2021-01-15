using Integration.Observability.Constants;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Integration.Observability.Helpers
{
    public static class LoggerHelper
    {
        /// <summary>
        /// Calculate LogLevel based on the process status. 
        /// </summary>
        /// <param name="status"></param>
        /// <returns>LogLevel</returns>
        public static LogLevel CalculateLogLevel(LoggingConstants.Status status)
        {
            switch (status)
            {
                case LoggingConstants.Status.Succeeded:
                    return LogLevel.Information;
                case LoggingConstants.Status.AttemptFailed:
                    return LogLevel.Warning;                    
                case LoggingConstants.Status.Failed:
                    return LogLevel.Error;
                case LoggingConstants.Status.Skipped:
                    return LogLevel.Warning;
                default:
                    return LogLevel.Information;
            }
        }
    }
}
