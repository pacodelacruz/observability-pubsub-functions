using System;
using System.Collections.Generic;
using System.Text;

namespace Integration.Observability.Constants
{
    public class ServiceBusConstants
    {
        /// <summary>
        /// Represent the settlement actions that can be actioned on a Service Bus message
        /// </summary>
        public enum SettlementActions
        {
            None,
            Abandon,
            Close,
            Complete,
            DeadLetter,
            Defer,
            RenewLock
        }

        /// <summary>
        /// Name of the custom user properties that can be added to Service Bus messages
        /// </summary>
        public enum MessageUserProperties
        {
            TraceId,
            BatchId,
            EntityId,
            Source,
            Timestamp
        }

    }
}
