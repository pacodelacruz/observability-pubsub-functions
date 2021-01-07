using System;
using System.Collections.Generic;
using System.Text;
using Integration.Observability.Constants;
using Microsoft.Extensions.Logging;

namespace Integration.Observability.Extensions
{
    public static class LoggerExtensions
    {
        private const string Template = "{Message}, {EntityType}, {Checkpoint}, {Status}, {CorrelationId}";

        public static void LogStructured(this ILogger logger, 
            LogLevel level, int eventId, LoggingConstants.CheckPoint checkpoint, LoggingConstants.Status status,
            LoggingConstants.EntityType entityType, string correlationId, string message = "")
        {
            logger.Log(level, new EventId(eventId), Template, message, entityType, checkpoint, status, correlationId);
        }

        public static void LogStructuredError(this ILogger logger, 
            Exception ex, int eventId, LoggingConstants.CheckPoint checkpoint, LoggingConstants.Status status,
            LoggingConstants.EntityType entityType, string correlationId, string message = "")
        {
            logger.Log(LogLevel.Error, new EventId(eventId), ex, Template, message, entityType, checkpoint, status, correlationId);
        }
    }
}
