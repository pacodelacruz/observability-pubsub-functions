using System;
using System.Collections.Generic;
using System.Text;

namespace Integration.Observability.Constants
{
    public class LoggingConstants
    {
        public enum CheckPoint
        {
            PublisherBatchReceipt,
            PublisherReceipt,
            PublisherDelivery,
            SubscriberReceipt,
            SubscriberDelivery
        }
        public class EventId
        {
            public const int PublisherInternalServerError = 10099;
            public const int PublisherBatchReceiptFailedBadRequest = 10090;
            public const int PublisherReceiptSucceeded = 21001;
            public const int PublisherDeliverySucceeded = 21050;
            public const int PublisherDeliveryFailed = 21083;
            public const int PublisherReceiptFailedBadRequest = 21082;
            public const int PublisherReceiptFailedUnauthorized = 21081;
            public const int SubscriberReceiptSucceeded = 21101;
            public const int SubscriberReceiptFailed = 21082;
            public const int SubscriberDeliverySucceeded = 21150;
            public const int SubscriberDeliverySkipped = 21170;
            public const int SubscriberDeliveryFailed = 21182;
            public const int SubscriberDeliveryFailedUserNotExists = 21181;
        }
        public enum Status
        {
            Succeeded,
            AttemptFail,
            Failed,
            Skipped
        }

        public enum EntityType
        {
            UserUpdateEvent,
            UserAddEvent
        }
    }
}
