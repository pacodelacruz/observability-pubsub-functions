using Integration.Observability.Constants;
using Integration.Observability.Extensions;
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
            try
            {
                string eventsAsJson = await new StreamReader(req.Body).ReadToEndAsync();

                // Do most of the processing in a separate method for testability. 
                var processResult = ProcessUserEventPublishing(eventsAsJson, ctx.InvocationId.ToString(), log);

                // For each debatched message
                foreach (var message in processResult.messages)
                {
                    message.UserProperties.TryGetValue(ServiceBusConstants.MessageUserProperties.EntityId.ToString(), out var entityId);

                    // Add the message to the queue Collector for delivery using the Azure Functions bindings
                    await queueCollector.AddAsync(message);

                    // Log PublisherDeliverySucceeded
                    log.LogStructured(LogLevel.Information,
                                      (int)LoggingConstants.EventId.PublisherDeliverySucceeded,
                                      LoggingConstants.SpanId.PublisherDelivery,
                                      LoggingConstants.Status.Succeeded,
                                      LoggingConstants.MessageType.UserUpdateEvent,
                                      processResult.userEventsMessage.Id,
                                      entityId.ToString());
                }

                return processResult.httpResponse;
            }
            catch (Exception ex)
            {
                {
                    // Log PublisherInternalServerError and return HTTP 500 with the invocation Id for correlation with the logged error message. 
                    log.LogStructuredError(ex, 
                                           (int)LoggingConstants.EventId.PublisherInternalServerError, 
                                           LoggingConstants.SpanId.Publisher, 
                                           LoggingConstants.Status.Failed, 
                                           LoggingConstants.MessageType.UserUpdateEvent, 
                                           "Unavailable", 
                                           message: ex.Message);

                    return new ObjectResult(new ApiResponse(StatusCodes.Status500InternalServerError, ctx.InvocationId.ToString(), "Internal Server Error")) {StatusCode = StatusCodes.Status500InternalServerError };
                }
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

            // Validate the payload
            if (!TryDeserialiseUserEvents(eventsAsJson, out var userEventsMessage))
            {
                // If paylod is not valid
                // Log PublisherBatchReceiptFailedBadRequest error due to invalid request body and return HTTP 400 with the invocation Id for correlation with the logged error message.
                log.LogStructured(LogLevel.Error,
                                  (int)LoggingConstants.EventId.PublisherBatchReceiptFailedBadRequest,
                                  LoggingConstants.SpanId.PublisherBatchReceipt,
                                  LoggingConstants.Status.Failed,
                                  LoggingConstants.MessageType.UserUpdateEvent,
                                  "Unavailable", "Invalid request body");

                // Return BadRequest
                return (new BadRequestObjectResult(new ApiResponse(StatusCodes.Status400BadRequest, invocationId, "Invalid request body")), null, null);
            }

            var userEvents = (List<UserEventDto>)userEventsMessage.Data;

            // Log PublisherBatchReceiptSucceeded
            log.LogStructured(LogLevel.Information,
                              (int)LoggingConstants.EventId.PublisherBatchReceiptSucceeded,
                              LoggingConstants.SpanId.PublisherBatchReceipt,
                              LoggingConstants.Status.Succeeded,
                              LoggingConstants.MessageType.UserUpdateEvent,
                              userEventsMessage.Id,
                              recordCount: userEvents.Count);

            // Debatch the message into multiple events and prepare them to be sent to Service Bus.
            foreach (var userEvent in userEvents)
            {
                // Log PublisherReceiptSucceeded
                log.LogStructured(LogLevel.Information,
                                  (int)LoggingConstants.EventId.PublisherReceiptSucceeded,
                                  LoggingConstants.SpanId.PublisherReceipt,
                                  LoggingConstants.Status.Succeeded,
                                  LoggingConstants.MessageType.UserUpdateEvent,
                                  userEventsMessage.Id,
                                  userEvent.Id.ToString());

                // Create Service Bus message
                var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userEvent));

                var userEventMessage = new Message(messageBody)
                {
                    MessageId = $"{userEventsMessage.Id}.{userEvent.Id}"
                };

                // Add user properties to the Service Bus message
                userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.TraceId.ToString(), invocationId);
                userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.BatchId.ToString(), userEventsMessage.Id);
                userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.EntityId.ToString(), userEvent.Id.ToString());
                userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.Source.ToString(), userEventsMessage.Source);
                userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.Timestamp.ToString(), userEvent.Timestamp.ToString("o"));

                // Add the message to the list
                messages.Add(userEventMessage);
            }

            return (new ObjectResult(new ApiResponse(StatusCodes.Status202Accepted, invocationId, "Accepted")) { StatusCode = StatusCodes.Status202Accepted }, messages, userEventsMessage);
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
