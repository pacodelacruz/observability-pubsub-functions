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
        private const string _template = "{Message}, {MessageType}, {SpanId}, {Status}, {CorrelationId}, {DeliveryCount}, {RecordCount}";

        public static void LogStructured(this ILogger logger,
            LogLevel level, int eventId, TracingConstants.SpanId spanId, TracingConstants.Status status,
            TracingConstants.MessageType messageType, string correlationId, string message = null, string deliveryCount = null, int? recordCount = null)
        {
            //string customTemplate = CustomiseTemplate(_template, recordCount, deliveryCount);
            logger.Log(level, new EventId(eventId), _template, message, messageType, spanId, status, correlationId, deliveryCount, recordCount);
        }

        public static void LogStructuredError(this ILogger logger,
            Exception ex, int eventId, TracingConstants.SpanId spanId, TracingConstants.Status status,
            TracingConstants.MessageType messageType, string correlationId, string message = null, string deliveryCount = null, int? recordCount = null)
        {
            //string customTemplate = CustomiseTemplate(_template, recordCount, deliveryCount);
            logger.Log(LogLevel.Error, new EventId(eventId), ex, _template, message, messageType, spanId, status, correlationId, deliveryCount, recordCount);
        }

        //private static string CustomiseTemplate(string originalTemplate, int? recordCount, int? deliveryCount)
        //{
        //    string customTemplate = originalTemplate;

        //    if (recordCount.HasValue)
        //        customTemplate = originalTemplate + ", {RecordCount}";
        //    if (deliveryCount.HasValue)
        //        customTemplate = originalTemplate + ", {DeliveryCount}";

        //    return customTemplate;
        //}
    }
}
