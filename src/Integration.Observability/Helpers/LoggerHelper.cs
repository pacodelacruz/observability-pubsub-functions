using Integration.Observability.Constants;
using Microsoft.Extensions.Logging;

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
                // When an attempt failed, but a retry is expected, return Warning
                case LoggingConstants.Status.AttemptFailed:
                // When a message is discarded due to business rules, return Warning
                case LoggingConstants.Status.Discarded:
                    return LogLevel.Warning;                    
                case LoggingConstants.Status.Failed:
                    return LogLevel.Error;
                default:
                    return LogLevel.Information;
            }
        }
    }
}
