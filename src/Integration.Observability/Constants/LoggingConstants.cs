using System;
using System.Collections.Generic;
using System.Text;

namespace Integration.Observability.Constants
{
    /// <summary>
    /// Constants used for Logging and Tracing
    /// </summary>
    public class LoggingConstants
    {
        /// <summary>
        /// To identify the tracing span checkpoints
        /// Follow the structure SpanId + CheckPoint (e.g. Start or End)
        /// </summary>
        public enum SpanCheckpointId
        {
            BatchPublisherStart,
            BatchPublisherEnd,
            PublisherStart,
            PublisherEnd,
            SubscriberStart,
            SubscriberEnd
        }

        /// <summary>
        /// Event Ids useful for querying, analysis and troubleshooting. 
        /// When granularity is required, different event ids can happen in the same combination of SpanCheckpointId and status. 
        /// Follow the structure SpanId + SpanStage + EventDescription
        /// </summary>
        public enum EventId
        {

            BatchPublisherReceiptSucceeded = 11000,
            BatchPublisherValidationBadRequest = 11090,
            BatchPublisherDeliverySucceeded = 11100,
            BatchPublisherInternalServerError = 11199,
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
            AttemptFailed,  // An attempt of the span process failed. A retry is expected. 
            Failed,         // The span process failed. A retry is not expected. 
            Discarded       // The span process was not performed due to business rules. 
        }

        /// <summary>
        /// The entity being processed in the span. 
        /// </summary>
        public enum MessageType
        {
            UserUpdateEventBatch,
            UserUpdateEvent
        }

        /// <summary>
        /// The business interfaceId
        /// </summary>
        public enum InterfaceId
        {
            UserEventPub01,
            UserEventSub01
        }
    }
}
