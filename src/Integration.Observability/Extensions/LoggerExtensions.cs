using System;
using System.Collections.Generic;
using System.Text;
using Integration.Observability.Constants;
using Microsoft.Extensions.Logging;

namespace Integration.Observability.Extensions
{
    /// <summary>
    /// ILogger extensions for structured logging using typed signatures.
    /// </summary>
    public static class LoggerExtensions
    {
        private const string _template = "{Message}, {InterfaceId}, {EntityType}, {EntityId}, {SpanCheckpointId}, {Status}, {BatchId}, {CorrelationId}, {DeliveryCount}, {RecordCount}";

        public static void LogStructured(this ILogger logger,
            LogLevel level, LoggingConstants.EventId eventId, LoggingConstants.SpanCheckpointId spanCheckpointId, LoggingConstants.Status status,
            LoggingConstants.InterfaceId interfaceId, LoggingConstants.MessageType entityType, string batchId, string correlationId, string entityId = null, string message = null, string deliveryCount = null, int? recordCount = null)
        {
            logger.Log(level, new EventId((int)eventId, eventId.ToString()), _template, message, interfaceId, entityType, entityId, spanCheckpointId, status, batchId, correlationId, deliveryCount, recordCount);
        }

        public static void LogStructuredError(this ILogger logger,
            Exception ex, LoggingConstants.EventId eventId, LoggingConstants.SpanCheckpointId spanCheckpointId, LoggingConstants.Status status,
            LoggingConstants.InterfaceId interfaceId, LoggingConstants.MessageType entityType, string batchId, string correlationId, string entityId = null, string message = null, string deliveryCount = null, int? recordCount = null)
        {
            logger.Log(LogLevel.Error, new EventId((int)eventId, eventId.ToString()), ex, _template, message, interfaceId, entityType, entityId, spanCheckpointId, status, batchId, correlationId, deliveryCount, recordCount);
        }
    }
}
