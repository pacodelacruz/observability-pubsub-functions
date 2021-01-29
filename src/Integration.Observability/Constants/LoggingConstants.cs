namespace Integration.Observability.Constants
{
    /// <summary>
    /// Constants used for Logging and Tracing
    /// </summary>
    public class LoggingConstants
    {
        /// <summary>
        /// To identify the tracing span checkpoints (e.g. start or finish of each span)
        /// Enum values follow the structure spanId + checkPoint
        /// </summary>
        public enum SpanCheckpointId
        {
            BatchPublisherStart,
            BatchPublisherFinish,
            PublisherStart,
            PublisherFinish,
            SubscriberStart,
            SubscriberFinish
        }

        /// <summary>
        /// Event Ids useful for querying, analysing, and troubleshooting tracing data. 
        /// Different eventIds can happen in the same combination of SpanCheckpointId and status to provide more granularity. 
        /// Follow the structure SpanId + SpanStage + EventDescription
        /// </summary>
        public enum EventId
        {

            BatchPublisherReceiptSucceeded = 11000,
            BatchPublisherValidationFailedBadRequest = 11090,
            BatchPublisherDeliverySucceeded = 11100,
            BatchPublisherProcessingFailedInternalServerError = 11199,
            PublisherReceiptSucceeded = 11200,
            PublisherDeliverySucceeded = 11300,
            SubscriberReceiptSucceeded = 11500,
            SubscriberDeliverySucceeded = 11600,
            SubscriberDeliveryDiscardedStaleMessage = 11680,
            SubscriberDeliveryFailedMissingDependency = 11688,
            SubscriberDeliveryFailedUnreachableTarget = 11689,
            SubscriberDeliveryFailedInvalidMessage = 11690,
            SubscriberDeliveryFailedException = 11699
        }

        /// <summary>
        /// The span execution status
        /// </summary>
        public enum Status
        {
            NotAvailable,   // The span status is not yet available
            Succeeded,      // The span process succeeded.
            AttemptFailed,  // An attempt of the span process failed. A retry for the message is expected. 
            Failed,         // The span process failed. A retry for the message is not expected. 
            Discarded       // The message processed in the span was discarded due to business rules. 
        }

        /// <summary>
        /// The entity being processed in the span. 
        /// Add when more message types are being processed. 
        /// </summary>
        public enum MessageType
        {
            UserUpdateEventBatch,
            UserUpdateEvent
        }

        /// <summary>
        /// The business interfaceId
        /// Add when more interfaces are being processed. 
        /// </summary>
        public enum InterfaceId
        {
            UserEventPub01,
            UserEventSub01
        }
    }
}
