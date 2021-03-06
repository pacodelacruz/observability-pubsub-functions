using Integration.Observability.Constants;
using Integration.Observability.Extensions;
using Integration.Observability.Helpers;
using Integration.Observability.PubSub.FnApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Integration.Observability.PubSub.FnApp
{
    public class UserUpdatedPublisher
    {
        private readonly IOptions<FunctionOptions> _options;

        public UserUpdatedPublisher(IOptions<FunctionOptions> options)
        {
            _options = options;
        }

        /// <summary>
        /// Receives a HTTP request with user events in the Cloud Events format, debatches the events and sends them to a Service Bus queue. 
        /// </summary>
        /// <param name="req">HTTP request</param>
        /// <param name="queueCollector">Service Bus message queue collector</param>
        /// <param name="ctx">Azure Function context</param>
        /// <param name="log">Logger</param>
        /// <returns>HTTP Response to client and Service Bus messages using the Azure Function bindings</returns>
        [FunctionName(nameof(UserUpdatedPublisher))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "userupdated")] HttpRequest req,
            [ServiceBus("%ServiceBusUserUpdateQueueName%", Connection = "ServiceBusConnectionString")] IAsyncCollector<Message> queueCollector,
            ExecutionContext ctx,
            ILogger log)
        {

            string batchId = ctx.InvocationId.ToString(); // batchId is defined with the Azure Function invocationId

            try
            {
                // Log BatchPublisherReceiptSucceeded
                log.LogStructured(LogLevel.Information,
                                  LoggingConstants.EventId.BatchPublisherReceiptSucceeded,
                                  LoggingConstants.SpanCheckpointId.BatchPublisherStart,
                                  LoggingConstants.Status.Succeeded,
                                  LoggingConstants.InterfaceId.UserEventPub01,
                                  LoggingConstants.EntityType.UserUpdateEventBatch,
                                  batchId: batchId,
                                  correlationId: null);

                string eventsAsJson = await new StreamReader(req.Body).ReadToEndAsync();

                //Archive the request body as an Azure Storage blob
                StorageHelper.ArchiveToBlob(eventsAsJson, _options.Value.StorageArchiveBlobContainer, $"{DateTime.Now:yyyy/MM/dd}/{ctx.InvocationId}.json", _options.Value.AzureWebJobsStorage);

                // Do most of the processing in a separate method for testability. 
                var processResult = ProcessUserEventPublishing(eventsAsJson, ctx.InvocationId.ToString(), log);

                if ((processResult.messages is null))
                {
                    // If paylod is not valid, log BatchPublisherValidationFailedBadRequest error due to invalid request body.
                    log.LogStructured(LogLevel.Error,
                                      LoggingConstants.EventId.BatchPublisherValidationFailedBadRequest,
                                      LoggingConstants.SpanCheckpointId.BatchPublisherFinish,
                                      LoggingConstants.Status.Failed,
                                      LoggingConstants.InterfaceId.UserEventPub01,
                                      LoggingConstants.EntityType.UserUpdateEventBatch,
                                      batchId: batchId,
                                      correlationId: null,
                                      message: "Invalid request body");
                }
                else
                {
                    // For each debatched message
                    foreach (var message in processResult.messages)
                    {
                        message.UserProperties.TryGetValue(ServiceBusConstants.MessageUserProperties.EntityId.ToString(), out var entityId);

                        // Add the message to the queue Collector for delivery using the Azure Functions bindings
                        await queueCollector.AddAsync(message);

                        // Log PublisherDeliverySucceeded
                        log.LogStructured(LogLevel.Information,
                                          LoggingConstants.EventId.PublisherDeliverySucceeded,
                                          LoggingConstants.SpanCheckpointId.PublisherFinish,
                                          LoggingConstants.Status.Succeeded,
                                          LoggingConstants.InterfaceId.UserEventPub01,
                                          LoggingConstants.EntityType.UserUpdateEvent,
                                          batchId: batchId,
                                          correlationId: message.CorrelationId,
                                          entityId: entityId.ToString());
                    }

                    // After debatching and sending the messages to the queue,
                    // log BatchPublisherDeliverySucceeded.
                    log.LogStructured(LogLevel.Information,
                                      LoggingConstants.EventId.BatchPublisherDeliverySucceeded,
                                      LoggingConstants.SpanCheckpointId.BatchPublisherFinish,
                                      LoggingConstants.Status.Succeeded,
                                      LoggingConstants.InterfaceId.UserEventPub01,
                                      LoggingConstants.EntityType.UserUpdateEventBatch,
                                      batchId: batchId,
                                      correlationId: null,
                                      entityId: processResult.userEventsMessage.Id,
                                      recordCount: processResult.messages.Count);
                }

                return processResult.httpResponse;
            }
            catch (Exception ex)
            {
                // Log BatchPublisherProcessingFailedInternalServerError and return HTTP 500 with the invocation Id for correlation with the logged error message. 
                log.LogStructuredError(ex,
                                       LoggingConstants.EventId.BatchPublisherProcessingFailedInternalServerError,
                                       LoggingConstants.SpanCheckpointId.BatchPublisherFinish,
                                       LoggingConstants.Status.Failed,
                                       LoggingConstants.InterfaceId.UserEventPub01,
                                       LoggingConstants.EntityType.UserUpdateEventBatch,
                                       batchId: batchId,
                                       correlationId: null,
                                       message: ex.Message);

                return new ObjectResult(new ApiResponse(StatusCodes.Status500InternalServerError, ctx.InvocationId.ToString(), "Internal Server Error")) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        /// <summary>
        /// Main logic of the Azure Function as a public method so that it can be tested in isolation. 
        /// Validates the payload, debatches the user events, and prepare the messages for Service Bus
        /// </summary>
        /// <param name="eventsAsJson">HTTP request body payload as a string, expected in the JSON format</param>
        /// <param name="invocationId">Azure Function ctx.invocationId for logging and troubleshooting purposes</param>
        /// <param name="log">ILogger object</param>
        /// <returns>httpResponse for the client, a list of messages for Service Bus and the parsed CloudEvent</returns>
        public (IActionResult httpResponse, List<Message> messages, CloudEvent userEventsMessage)
            ProcessUserEventPublishing(string eventsAsJson, string invocationId, ILogger log)
        {
            var messages = new List<Message>();
            string batchId = invocationId; // batchId is defined with the Azure Function invocationId
            string correlationId;
            string messageId;

            // Validate the payload
            if (!TryDeserialiseUserEvents(eventsAsJson, out var userEventsMessage))
            {

                // If paylod is not valid, return HTTP 400 BadRequest with the invocation Id for correlation.
                return (
                    new BadRequestObjectResult(
                        new ApiResponse(StatusCodes.Status400BadRequest, invocationId, "Invalid request body")),
                    null, null);
            }

            var userEvents = (List<UserEventDto>)userEventsMessage.Data;

            // Debatch the message into multiple events and prepare them to be sent to Service Bus.
            foreach (var userEvent in userEvents)
            {
                // correlationId is defined with the invocationId, the CloudEvents messageId, and the individial message eventId
                correlationId = $"{invocationId}|{userEventsMessage.Id}|{userEvent.Id}";

                // messageId is defined with the source's CloudEventId and eventId. 
                // This could be used for idempotence or de-duplication purposes. 
                messageId = $"{userEventsMessage.Id}|{userEvent.Id}";

                // Log PublisherReceiptSucceeded
                log.LogStructured(LogLevel.Information,
                                  LoggingConstants.EventId.PublisherReceiptSucceeded,
                                  LoggingConstants.SpanCheckpointId.PublisherStart,
                                  LoggingConstants.Status.Succeeded,
                                  LoggingConstants.InterfaceId.UserEventPub01,
                                  LoggingConstants.EntityType.UserUpdateEvent,
                                  batchId: batchId,
                                  correlationId: correlationId,
                                  entityId: userEvent.Id.ToString());

                var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userEvent));

                // Create Service Bus message
                // Add message properties, including those cross-span (bagagge items) tracing metadata properties 
                var userEventMessage = new Message(messageBody)
                {
                    MessageId = messageId,
                    CorrelationId = correlationId
                };

                // Add user properties to the Service Bus message, including those cross-span (bagagge items) tracing metadata properties 
                userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.BatchId.ToString(), batchId);
                userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.EntityId.ToString(), userEvent.Id.ToString());
                userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.Source.ToString(), userEventsMessage.Source);
                userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.Timestamp.ToString(), userEvent.Timestamp.ToString("o"));

                // Add the message to the list
                messages.Add(userEventMessage);
            }

            return (
                new ObjectResult(new ApiResponse(StatusCodes.Status202Accepted, invocationId, "Accepted"))
                { StatusCode = StatusCodes.Status202Accepted },
                messages,
                userEventsMessage);
        }

        /// <summary>
        /// Try to deserialise a json message as a string to user events in the Cloud Events specification. 
        /// </summary>
        /// <param name="message">Json message as a string expected to conform with the Cloud Events specification</param>
        /// <param name="userEventsMessage">Out parameter: User events as a Cloud Event object</param>
        /// <returns>bool: if string message could be deserialised</returns>
        public bool TryDeserialiseUserEvents(string message, out CloudEvent userEventsMessage)
        {
            try
            {
                userEventsMessage = JsonSerializer.Deserialize<CloudEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userEventsMessage?.Id is null || userEventsMessage?.Data is null)
                {
                    return false;
                }
                var userEvents = JsonSerializer.Deserialize<List<UserEventDto>>(userEventsMessage.Data.ToString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (userEvents.Count < 1)
                {
                    return false;
                }

                userEventsMessage.Data = userEvents;
                return true;
            }
            catch (Exception ex)
            {
                userEventsMessage = null;
                return false;
            }
        }
    }
}
