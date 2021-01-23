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

        /// <summary>
        /// Receives a Service Bus message and process it. 
        /// </summary>
        /// <param name="userEventMessage"></param>
        /// <param name="lockToken"></param>
        /// <param name="messageReceiver"></param>
        /// <param name="deliveryCount"></param>
        /// <param name="log"></param>
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
            var processResult = ProcessUserEventSubscription(userEventMessage, deliveryCount, log);

            // Settle the Service Bus message based on the process result. 
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
        public (ServiceBusConstants.SettlementActions settlementAction, LoggingConstants.EventId eventId, LoggingConstants.Status status, string message)
            ProcessUserEventSubscription(Message userEventMessage, int deliveryCount, ILogger log)
        {
            var settlementAction = ServiceBusConstants.SettlementActions.None;
            string deliveryCountToLog = $"{deliveryCount}/{_options.Value.ServiceBusUserUpdateQueueMaxDeliveryCount}";

            // Get message properties, read, and deserialise body. 
            userEventMessage.UserProperties.TryGetValue(ServiceBusConstants.MessageUserProperties.BatchId.ToString(), out var batchId);
            userEventMessage.UserProperties.TryGetValue(ServiceBusConstants.MessageUserProperties.EntityId.ToString(), out var entityId);
            var messageBody = Encoding.UTF8.GetString(userEventMessage.Body);
            var userEvent = JsonSerializer.Deserialize<UserEventDto>(messageBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Log SubscriberReceiptSucceeded
            log.LogStructured(LogLevel.Information,
                              LoggingConstants.EventId.SubscriberReceiptSucceeded,
                              LoggingConstants.SpanCheckpointId.SubscriberStart,
                              LoggingConstants.Status.Succeeded,
                              LoggingConstants.InterfaceId.UserEventSub01, 
                              LoggingConstants.MessageType.UserUpdateEvent,
                              batchId: batchId.ToString(),
                              correlationId: userEventMessage.CorrelationId,
                              entityId: entityId.ToString(),
                              deliveryCount: deliveryCountToLog);

            // Check if the current delivery count equals the max delivery count for the queue. 
            bool isLastAttempt = (deliveryCount == _options.Value.ServiceBusUserUpdateQueueMaxDeliveryCount);
            var processResultStatus = LoggingConstants.Status.NotAvailable;

            try
            {
                // Simulate delivery to target system
                (LoggingConstants.EventId eventId, LoggingConstants.Status status, string message, bool doNotRetry) processResult =
                    DeliverToTargetSystem(userEvent);

                processResultStatus = processResult.status;

                // If it is not the last delivery attempt, the process did not return doNotRetry and the process failed, then change from Failed to AttemptFailed
                if (!isLastAttempt && !processResult.doNotRetry && processResult.status == LoggingConstants.Status.Failed)
                    processResultStatus = LoggingConstants.Status.AttemptFailed;

                // Log process result
                log.LogStructured(LoggerHelper.CalculateLogLevel(processResult.status),
                                  processResult.eventId,
                                  LoggingConstants.SpanCheckpointId.SubscriberEnd,
                                  processResultStatus,
                                  LoggingConstants.InterfaceId.UserEventSub01, 
                                  LoggingConstants.MessageType.UserUpdateEvent,
                                  batchId: batchId.ToString(),
                                  correlationId: userEventMessage.CorrelationId,
                                  entityId: entityId.ToString(),
                                  message: processResult.message,
                                  deliveryCount: deliveryCountToLog);

                // If successfully delivered or skipped because not relevant, complete the message. 
                if (processResultStatus == LoggingConstants.Status.Succeeded || processResultStatus == LoggingConstants.Status.Discarded)
                {
                    settlementAction = ServiceBusConstants.SettlementActions.Complete;
                }
                // If failed and doNotRetry (e.g. a poisoned or invalid message), then deadletter the message. 
                else if (processResultStatus == LoggingConstants.Status.Failed && processResult.doNotRetry)
                {
                    settlementAction = ServiceBusConstants.SettlementActions.DeadLetter;
                }
                return (settlementAction, processResult.eventId, processResultStatus, processResult.message);

            }
            catch (Exception ex)
            {
                // Log exception
                var failedStatus = isLastAttempt ? LoggingConstants.Status.Failed : LoggingConstants.Status.AttemptFailed;

                // Log SubscriberDeliveryFailedException 
                log.LogStructuredError(ex,
                                       LoggingConstants.EventId.SubscriberDeliveryFailedException,
                                       LoggingConstants.SpanCheckpointId.SubscriberEnd,
                                       failedStatus,
                                       LoggingConstants.InterfaceId.UserEventSub01, 
                                       LoggingConstants.MessageType.UserUpdateEvent,
                                       batchId: batchId?.ToString(),
                                       correlationId: userEventMessage?.CorrelationId,
                                       entityId: entityId?.ToString(),
                                       message: ex.Message,
                                       deliveryCount: deliveryCountToLog);

                return (settlementAction, LoggingConstants.EventId.SubscriberDeliveryFailedException, failedStatus, ex.Message);
            }
        }

        /// <summary>
        /// Simulate the delivery a target system of a user event which can throw exceptions in certain conditions
        /// </summary>
        /// <param name="userEvent"></param>
        /// <returns>The status of the process</returns>
        private static (LoggingConstants.EventId eventId, LoggingConstants.Status, string message, bool doNotRetry) DeliverToTargetSystem(UserEventDto userEvent)
        {

            Random randomGenerator = new Random();
            int randomNumber = randomGenerator.Next(0, 2);

            if (userEvent.PhoneNumber.EndsWith("99"))
            {
                //Simulate an unhandled exception.
                throw new ApplicationException("Catastrophic failure");
            }
            else
            if (userEvent.PhoneNumber.EndsWith("09"))
            {
                // Simulate a poison or invalid message. No need to retry. 
                return (LoggingConstants.EventId.SubscriberDeliveryFailedInvalidMessage,
                        LoggingConstants.Status.Failed,
                        "Missing required fields",
                        doNotRetry: true);
            }
            else if (userEvent.PhoneNumber.EndsWith("08"))
            {
                // Simulate a stale message as described in: https://platform.deloitte.com.au/articles/enterprise-integration-patterns-on-azure-endpoints#stale-message
                return (LoggingConstants.EventId.SubscriberDeliveryDiscardedStaleMessage,
                        LoggingConstants.Status.Discarded,
                        "Stale message",
                        doNotRetry: true);
            }
            else if (userEvent.PhoneNumber.EndsWith("07"))
            {
                if (randomNumber == 0)
                {
                    // Simulate a transient error. A dependency is not ready in the target system. 
                    return (LoggingConstants.EventId.SubscriberDeliveryFailedMissingDependency,
                            LoggingConstants.Status.Failed,
                            "Dependency entity not available",
                            doNotRetry: false);
                }
                else
                {
                    // Simluate successful delivery to target system.
                    return (LoggingConstants.EventId.SubscriberDeliverySucceeded,
                            LoggingConstants.Status.Succeeded,
                            null,
                            doNotRetry: true);
                }
            }
            else if (userEvent.PhoneNumber.EndsWith("06"))
            {
                // Simulate a transient error. The target system is unreachable. 
                return (LoggingConstants.EventId.SubscriberDeliveryFailedUnreachableTarget,
                        LoggingConstants.Status.Failed,
                        "Unable to reach system X, error: 'inner exception'",
                        doNotRetry: false);
            }
            else
            {   // Simulate successful delivery to target system.
                return (LoggingConstants.EventId.SubscriberDeliverySucceeded,
                        LoggingConstants.Status.Succeeded,
                        null,
                        doNotRetry: true);
            }
        }
    }
}
