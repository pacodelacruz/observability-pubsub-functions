using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Integration.Observability.PubSub.FnApp.Models;
using Integration.Observability.Constants;
using Integration.Observability.Extensions;
using System.Collections.Generic;

namespace Integration.Observability.PubSub.Functions
{
    public class UserUpdatedPublisher
    {
        [FunctionName(nameof(UserUpdatedPublisher))]
        //[return: ServiceBus("%ServiceBusUserUpdateQueueName%", Connection = "ServiceBusConnectionString")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "userupdated")] HttpRequest req,
            ExecutionContext ctx,
            ILogger log)
        {
            try
            {
                string eventsAsJson = await new StreamReader(req.Body).ReadToEndAsync();

                if (!TrySerialiseUserEvents(eventsAsJson, out var userEvents))
                {
                    log.LogStructured(LogLevel.Error, LoggingConstants.EventId.PublisherBatchReceiptFailedBadRequest, LoggingConstants.CheckPoint.PublisherBatchReceipt, LoggingConstants.Status.Failed, LoggingConstants.EntityType.UserUpdateEvent, "unavailable", "Invalid request body");
                    
                    return new BadRequestObjectResult(new ApiResponse(400, ctx.InvocationId.ToString(), "Invalid body"));
                }
                return new AcceptedResult();
            }
            catch (Exception ex)
            {
                {
                    log.LogStructuredError(ex, LoggingConstants.EventId.PublisherInternalServerError, LoggingConstants.CheckPoint.PublisherBatchReceipt, LoggingConstants.Status.Failed, LoggingConstants.EntityType.UserUpdateEvent, "unavailable", ex.Message);
                    throw;
                }
            }
        }

        public bool TrySerialiseUserEvents(string message, out CloudEvent userEventsMessage)
        {
            try
            {
                userEventsMessage = JsonSerializer.Deserialize<CloudEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (userEventsMessage?.Id is null || userEventsMessage?.Data is null)
                {
                    return false;
                }
                var userEvents = JsonSerializer.Deserialize<List<UserDto>>(userEventsMessage.Data.ToString(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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
