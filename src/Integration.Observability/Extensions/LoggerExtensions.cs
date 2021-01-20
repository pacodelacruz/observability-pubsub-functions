﻿using System;
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
        private const string _template = "{Message}, {EntityType}, {EntityId}, {SpanId}, {Status}, {BatchId}, {CorrelationId}, {DeliveryCount}, {RecordCount}";

        public static void LogStructured(this ILogger logger,
            LogLevel level, int eventId, LoggingConstants.SpanId spanId, LoggingConstants.Status status,
            LoggingConstants.MessageType entityType, string batchId, string correlationId, string entityId = null, string message = null, string deliveryCount = null, int? recordCount = null)
        {
            logger.Log(level, new EventId(eventId), _template, message, entityType, entityId, spanId, status, correlationId, batchId, deliveryCount, recordCount);
        }

        public static void LogStructuredError(this ILogger logger,
            Exception ex, int eventId, LoggingConstants.SpanId spanId, LoggingConstants.Status status,
            LoggingConstants.MessageType entityType, string batchId, string correlationId, string entityId = null, string message = null, string deliveryCount = null, int? recordCount = null)
        {
            logger.Log(LogLevel.Error, new EventId(eventId), ex, _template, message, entityType, entityId, spanId, status, correlationId, batchId, deliveryCount, recordCount);
        }
    }
}
