using System;
using System.Collections.Generic;
using System.Text;

namespace Integration.Observability.Constants
{
    public class TracingConstants
    {
        public enum SpanId
        {
            Publisher, // The publisher interface span. Only for those tracing events that cannot be specific.
            PublisherBatchReceipt, // Span related to the receipt of a batch message in the publisher interface.
            PublisherReceipt, // Span related to the receipt of an individual message in the publisher interface.
            PublisherDelivery, // Span related to the publishing of an individual message in the publisher interface.
            Subscriber, // The subscriber interface span. Only for those tracing events that cannot be specific.
            SubscriberReceipt, // Span related to the receipt of a message in the subscriber interface.
            SubscriberDelivery // Span related to the delivery of a message in the subscriber interface.
        }

        /// <summary>
        /// Event Ids useful for querying, analysis and troubleshooting. 
        /// When granularity is required, different event ids can happen in the same combination of span and status. 
        /// </summary>
        public enum EventId
        {
            
            PublisherBatchReceiptSucceeded = 11000,         //PublisherBatch 110##
            PublisherBatchReceiptFailedBadRequest = 11090,
            PublisherInternalServerError = 11099,
            PublisherReceiptSucceeded = 11100,              //PublisherReceipt 111##
            PublisherDeliverySucceeded = 11200,             //PublisherDelivery 112##
            PublisherDeliveryFailed = 11290,
            SubscriberReceiptSucceeded = 11500,             //SubscriberReceipt 115##
            SubscriberReceiptFailed = 11590,                //SubscriberDelivery 116##
            SubscriberDeliverySucceeded = 11600,
            SubscriberDeliverySkippedStaleMessage = 11680,
            SubscriberDeliveryFailedMissingDependency = 11688,
            SubscriberDeliveryUnreachableTarget = 11689,
            SubscriberDeliveryFailedInvalidMessage = 11690,
            SubscriberDeliveryFailedException = 11699 
        }

        /// <summary>
        /// The final status of the span
        /// </summary>
        public enum Status
        {
            Succeeded, // The span operation succeeded.
            AttemptFailed, // An attempt of the span operation failed. A retry is expected. 
            Failed, // The span operation failed. A retry is not expected. 
            Skipped // The span operation was not performed due to business rules. 
        }

        /// <summary>
        /// The entity being processed in the span. 
        /// </summary>
        public enum MessageType
        {
            UserUpdateEvent
        }
    }
}
