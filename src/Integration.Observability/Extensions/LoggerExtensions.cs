using Integration.Observability.Constants;
using Microsoft.Extensions.Logging;
using System;

namespace Integration.Observability.Extensions
{
    /// <summary>
    /// ILogger extensions for structured logging using typed signatures.
    /// </summary>
    public static class LoggerExtensions
    {
        // Template used for structured logging
        private const string _template = "{Message}, {InterfaceId}, {EntityType}, {EntityId}, {SpanCheckpointId}, {Status}, {BatchId}, {CorrelationId}, {DeliveryCount}, {RecordCount}";

        /// <summary>
        /// Extension method that creates a structured logging record
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="level">LogLevel</param>
        /// <param name="eventId">Specific tracing event that helps to query, analyse, and troubleshoot the solution with granularity</param>
        /// <param name="spanCheckpointId">The tracing span checkpoint (e.g. start or finish of each span)</param>
        /// <param name="status">The span checkpoint execution status</param>
        /// <param name="interfaceId">The business interfaceId</param>
        /// <param name="entityType">The entity type being processed</param>
        /// <param name="batchId">Batch identifier to correlate individual messages to the original batch. It is highly recommended when using the splitter pattern.</param>
        /// <param name="correlationId">Tracing correlation identifier of an individual message.</param>
        /// <param name="entityId">Business identifier of the entity in the message. This together with the EntityType key-value pair allow to filter or query tracing events for messages related to a particular entity</param>
        /// <param name="message">Log event description message</param>
        /// <param name="deliveryCount">Only applicable to subscriber events of individual messages. Captures the number of times the message has been attempted to be delivered</param>
        /// <param name="recordCount">Only applicable to batch events. Captures the number of individual messages or records that are present in the batch</param>
        public static void LogStructured(this ILogger logger,
            LogLevel level, LoggingConstants.EventId eventId, LoggingConstants.SpanCheckpointId spanCheckpointId, LoggingConstants.Status status,
            LoggingConstants.InterfaceId interfaceId, LoggingConstants.EntityType entityType, string batchId, string correlationId, string entityId = null, string message = null, string deliveryCount = null, int? recordCount = null)
        {
            logger.Log(level, new EventId((int)eventId, eventId.ToString()), _template, message, interfaceId, entityType, entityId, spanCheckpointId, status, batchId, correlationId, deliveryCount, recordCount);
        }
        /// <summary>
        /// Extension method that creates an exception structured logging record
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="ex">Exception object</param>
        /// <param name="eventId">Specific tracing event that helps to query, analyse, and troubleshoot the solution with granularity</param>
        /// <param name="spanCheckpointId">The tracing span checkpoint (e.g. start or finish of each span)</param>
        /// <param name="status">The span checkpoint execution status</param>
        /// <param name="interfaceId">The business interfaceId</param>
        /// <param name="entityType">The entity type being processed</param>
        /// <param name="batchId">Batch identifier to correlate individual messages to the original batch. It is highly recommended when using the splitter pattern.</param>
        /// <param name="correlationId">Tracing correlation identifier of an individual message.</param>
        /// <param name="entityId">Business identifier of the entity in the message. This together with the EntityType key-value pair allow to filter or query tracing events for messages related to a particular entity</param>
        /// <param name="message">Log event description message</param>
        /// <param name="deliveryCount">Only applicable to subscriber events of individual messages. Captures the number of times the message has been attempted to be delivered</param>
        /// <param name="recordCount">Only applicable to batch events. Captures the number of individual messages or records that are present in the batch</param>
        public static void LogStructuredError(this ILogger logger,
            Exception ex, LoggingConstants.EventId eventId, LoggingConstants.SpanCheckpointId spanCheckpointId, LoggingConstants.Status status,
            LoggingConstants.InterfaceId interfaceId, LoggingConstants.EntityType entityType, string batchId, string correlationId, string entityId = null, string message = null, string deliveryCount = null, int? recordCount = null)
        {
            logger.Log(LogLevel.Error, new EventId((int)eventId, eventId.ToString()), ex, _template, message, interfaceId, entityType, entityId, spanCheckpointId, status, batchId, correlationId, deliveryCount, recordCount);
        }
    }
}
