using Integration.Observability.Constants;
using Integration.Observability.Extensions;
using Integration.Observability.Helpers;
using Integration.Observability.PubSub.FnApp.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Text.Json;

namespace Integration.Observability.PubSub.FnApp
{
    public class UserUpdatedSubscriber
    {
        private readonly IOptions<FunctionOptions> _options;

        public UserUpdatedSubscriber(IOptions<FunctionOptions> options)
        {
            _options = options;
        }

    [FunctionName(nameof(UserUpdatedSubscriber))]
        public async void Run(
            [ServiceBusTrigger("%ServiceBusUserUpdateQueueName%", Connection = "ServiceBusConnectionString")] 
            Message userEventMessage, 
            string lockToken,
            MessageReceiver messageReceiver,
            int deliveryCount,
            ILogger log)
        {
            // Do most of the processing in a separate method for testability. 
            var processResult = ProcessUserEvent(userEventMessage, deliveryCount, log);

            // Settle the message based on the process result. 
            switch (processResult.settlementAction)
            {
                case ServiceBusConstants.SettlementActions.None:
                    // Do not settle the message so that it keeps the lock until it expires. 
                    break;
                case ServiceBusConstants.SettlementActions.Abandon:
                    // Abandon the message so that the lock is released immediately. 
                    await messageReceiver.AbandonAsync(lockToken);
                    break;
                case ServiceBusConstants.SettlementActions.Complete:
                    // Complete the message so that it is removed from the queue. 
                    await messageReceiver.CompleteAsync(lockToken);
                    break;
                case ServiceBusConstants.SettlementActions.DeadLetter:
                    // Deadletter the message so that it is not retried. 
                    await messageReceiver.DeadLetterAsync(lockToken, processResult.message);
                    break;
                default:
                    // Do not settle the message so that it keeps the lock until it expires. 
                    break;
            }
        }

        /// <summary>
        /// Main logic of the Azure Function as a public method so that it can be tested in isolation. 
        /// </summary>
        /// <param name="userEventMessage"></param>
        /// <param name="deliveryCount"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public (ServiceBusConstants.SettlementActions settlementAction, TracingConstants.EventId eventId, TracingConstants.Status status, string message)
            ProcessUserEvent(Message userEventMessage, int deliveryCount, ILogger log)
        {
            var settlementAction = ServiceBusConstants.SettlementActions.None;
            string deliveryCountToLog = $"{deliveryCount}/{_options.Value.ServiceBusUserUpdateQueueMaxDeliveryCount}";

            // Get message properties, read, and deserialise body. 
            userEventMessage.UserProperties.TryGetValue(ServiceBusConstants.MessageUserProperties.BatchId.ToString(), out var eventBatchCorrelationId);
            var messageBody = Encoding.UTF8.GetString(userEventMessage.Body);
            var userEvent = JsonSerializer.Deserialize<UserEventDto>(messageBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Log SubscriberReceiptSucceeded
            log.LogStructured(LogLevel.Information,
                              (int)TracingConstants.EventId.SubscriberReceiptSucceeded,
                              TracingConstants.SpanId.SubscriberReceipt,
                              TracingConstants.Status.Succeeded,
                              TracingConstants.MessageType.UserUpdateEvent,
                              eventBatchCorrelationId.ToString(),
                              deliveryCount: deliveryCountToLog);

            // Check if the current delivery count equals the max delivery count for the queue. 
            bool isLastAttempt = (deliveryCount == _options.Value.ServiceBusUserUpdateQueueMaxDeliveryCount);
            var processResultStatus = TracingConstants.Status.NotAvailable;

            try
            {
                (TracingConstants.EventId eventId, TracingConstants.Status status, string message, bool doNotRetry) processResult = 
                    DeliverToTargetSystem(userEvent);

                processResultStatus = processResult.status;

                // If the process did not return doNotRetry and it is not the last attemp, then change from Failed to AttemptFailed
                if (!processResult.doNotRetry && processResult.status == TracingConstants.Status.Failed && !isLastAttempt)
                    processResultStatus = TracingConstants.Status.AttemptFailed;

                log.LogStructured(LoggerHelper.CalculateLogLevel(processResult.status),
                                  (int)processResult.eventId,
                                  TracingConstants.SpanId.SubscriberDelivery,
                                  processResultStatus,
                                  TracingConstants.MessageType.UserUpdateEvent,
                                  eventBatchCorrelationId.ToString(),
                                  message: processResult.message,
                                  deliveryCount: deliveryCountToLog);

                // If successfully delivered or skipped because not relevant, complete the message. 
                if (processResultStatus == TracingConstants.Status.Succeeded || processResultStatus == TracingConstants.Status.Skipped)
                {
                    settlementAction = ServiceBusConstants.SettlementActions.Complete;
                }
                // If failed and doNotRetry (e.g. a poisoned or invalid message), then deadletter the message. 
                else if (processResultStatus == TracingConstants.Status.Failed && processResult.doNotRetry)
                {
                    settlementAction = ServiceBusConstants.SettlementActions.DeadLetter;
                }
                return (settlementAction, processResult.eventId, processResultStatus, processResult.message);

            }
            catch (Exception ex)
            {
                // Log exception and then throw as is. 
                var failedStatus = isLastAttempt ? TracingConstants.Status.Failed : TracingConstants.Status.AttemptFailed;

                // Log SubscriberDeliveryFailedException 
                log.LogStructuredError(ex, 
                                       (int)TracingConstants.EventId.SubscriberDeliveryFailedException,        
                                       TracingConstants.SpanId.SubscriberDelivery, 
                                       failedStatus, 
                                       TracingConstants.MessageType.UserUpdateEvent, 
                                       eventBatchCorrelationId.ToString(), 
                                       ex.Message, 
                                       deliveryCount: deliveryCountToLog);

                throw ex;
            }
        }

        /// <summary>
        /// Simulate the delivery a target system of a user event which can throw exceptions in certain conditions
        /// </summary>
        /// <param name="userEvent"></param>
        /// <returns>The status of the process</returns>
        private static (TracingConstants.EventId eventId, TracingConstants.Status, string message, bool doNotRetry) DeliverToTargetSystem(UserEventDto userEvent)
        {
            if (userEvent.PhoneNumber.EndsWith("99"))
            {
                // Simulate an unhandled exception.
                throw new ApplicationException("Catastrophic failure");
            }
            else if (userEvent.PhoneNumber.EndsWith("09"))
            {
                // No need to retry, e.g. a poison or invalid message. 
                return (TracingConstants.EventId.SubscriberDeliveryFailedInvalidMessage, 
                        TracingConstants.Status.Failed, 
                        "Missing required fields",
                        doNotRetry: true);
            }
            else if (userEvent.PhoneNumber.EndsWith("08"))
            {
                // The message is stale as described in: https://platform.deloitte.com.au/articles/enterprise-integration-patterns-on-azure-endpoints#stale-message
                return (TracingConstants.EventId.SubscriberDeliverySkippedStaleMessage, 
                        TracingConstants.Status.Skipped, 
                        "Stale message",
                        doNotRetry: true);
            }
            else if (userEvent.PhoneNumber.EndsWith("07"))
            {
                // A transient error is received. A dependency is not ready in the target system. 
                return (TracingConstants.EventId.SubscriberDeliveryFailedMissingDependency,
                        TracingConstants.Status.Failed, 
                        "Dependency entity not available",
                        doNotRetry: false);
            }
            else if (userEvent.PhoneNumber.EndsWith("06"))
            {
                // A transient error is received. The target system is unreachable. 
                return (TracingConstants.EventId.SubscriberDeliveryUnreachableTarget,
                        TracingConstants.Status.Failed,
                        "Unable to reach system X, error: 'inner exception'",
                        doNotRetry: false);
            }
            else
            {   // Delivery to target system was successful
                return (TracingConstants.EventId.SubscriberDeliverySucceeded,
                        TracingConstants.Status.Succeeded,
                        null,
                        doNotRetry: true);
            }
        }
    }
}
