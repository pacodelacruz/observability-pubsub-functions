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

                // TODO: Log every payload to blob. 
                
                if (!TryDeserialiseUserEvents(eventsAsJson, out var userEventsMessage))
                {
                    // Log PublisherBatchReceiptFailedBadRequest error due to invalid request body and return HTTP 400 with the invocation Id for correlation with the logged error message.

                    log.LogStructured(LogLevel.Error, (int)TracingConstants.EventId.PublisherBatchReceiptFailedBadRequest, TracingConstants.SpanId.PublisherBatchReceipt, TracingConstants.Status.Failed, TracingConstants.MessageType.UserUpdateEvent, "Unavailable", "Invalid request body");
                    
                    return new BadRequestObjectResult(new ApiResponse(StatusCodes.Status400BadRequest, ctx.InvocationId.ToString(), "Invalid body"));
                }

                var userEvents = (List<UserEventDto>)userEventsMessage.Data;

                // Log PublisherBatchReceiptSucceeded
                log.LogStructured(LogLevel.Information, (int)TracingConstants.EventId.PublisherBatchReceiptSucceeded, TracingConstants.SpanId.PublisherBatchReceipt, TracingConstants.Status.Succeeded, TracingConstants.MessageType.UserUpdateEvent, userEventsMessage.Id, recordCount: userEvents.Count);

                // Debatch the message into multiple events and send them to Service Bus
                foreach (var userEvent in userEvents)
                {
                    // Log PublisherReceiptSucceeded
                    log.LogStructured(LogLevel.Information, (int)TracingConstants.EventId.PublisherReceiptSucceeded, TracingConstants.SpanId.PublisherReceipt, TracingConstants.Status.Succeeded, TracingConstants.MessageType.UserUpdateEvent, userEventsMessage.Id);

                    // Create Service Bus message
                    var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userEvent));                    

                    var userEventMessage = new Message(messageBody) { 
                        MessageId = $"{userEventsMessage.Id}.{userEvent.Id}"
                    };

                    // Add user properties to the Service Bus message
                    userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.TraceId.ToString(), ctx.InvocationId.ToString());
                    userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.BatchId.ToString(), userEventsMessage.Id);
                    userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.EntityId.ToString(), userEvent.Id.ToString());
                    userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.Source.ToString(), userEventsMessage.Source);
                    userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.Timestamp.ToString(), userEvent.Timestamp.ToString("o"));

                    // Add the message to the queue Collector for delivery
                    await queueCollector.AddAsync(userEventMessage);

                    // Log PublisherDeliverySucceeded
                    log.LogStructured(LogLevel.Information, (int)TracingConstants.EventId.PublisherDeliverySucceeded, TracingConstants.SpanId.PublisherDelivery, TracingConstants.Status.Succeeded, TracingConstants.MessageType.UserUpdateEvent, userEventsMessage.Id);
                }

                return new AcceptedResult();
            }
            catch (Exception ex)
            {
                {
                    // Log PublisherInternalServerError and return HTTP 500 with the invocation Id for correlation with the logged error message. 

                    log.LogStructuredError(ex, (int)TracingConstants.EventId.PublisherInternalServerError, TracingConstants.SpanId.Publisher, TracingConstants.Status.Failed, TracingConstants.MessageType.UserUpdateEvent, "Unavailable", ex.Message);

                    return new ObjectResult(new ApiResponse(StatusCodes.Status500InternalServerError, ctx.InvocationId.ToString(), "Internal Server Error")) {StatusCode = StatusCodes.Status500InternalServerError };
                }
            }
        }

        /// <summary>
        /// Try to deserialise a json message as a string to user events in the Cloud Events specification. 
        /// </summary>
        /// <param name="message">Json message as a string expected to conform with the Cloud Events specification</param>
        /// <param name="userEventsMessage">Out parameter: User events as a Cloud Event object</param>
        /// <returns></returns>
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
