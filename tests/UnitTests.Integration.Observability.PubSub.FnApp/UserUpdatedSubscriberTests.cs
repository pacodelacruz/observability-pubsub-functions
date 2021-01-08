using Integration.Observability.Constants;
using Integration.Observability.PubSub.FnApp;
using Integration.Observability.PubSub.FnApp.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnitTests.Integration.Observability.PubSub.FnApp.Helpers;
using Xunit;

namespace UnitTests.Integration.Observability.PubSub.FnApp
{
    public class UserUpdatedSubscriberTests
    {
        private IOptions<FunctionOptions> _options;
        private UserUpdatedSubscriber _userUpdatedSubscriber;
        private ILogger _consoleLogger;

        public UserUpdatedSubscriberTests()
        {
            // Load configuration options from the app settings of the test project. 
            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", false)
               .Build();

            _options = Options.Create(configuration.GetSection("Values").Get<FunctionOptions>());

            _userUpdatedSubscriber = new UserUpdatedSubscriber(_options);

            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _consoleLogger = loggerFactory.CreateLogger<UserUpdatedSubscriber>();
        }
           
        //TODO: Add test cases. 
        [Theory]
        [InlineData("UserEventComplete01.json", 1, ServiceBusConstants.SettlementActions.Complete, TracingConstants.EventId.SubscriberDeliverySucceeded, null)]
        public void When_ProcessUserEvent_ReturnExpectedResult(
            string payloadFileName, 
            int deliveryCount, 
            ServiceBusConstants.SettlementActions settlementAction, 
            TracingConstants.EventId eventId, 
            string message)
        {
            //Arrange
            var payload = TestDataHelper.GetTestDataStringFromFile("UserEvents", payloadFileName);
            var userEventMessage = CreateServiceBusMessage(payload);

            // Act
            var processResult = _userUpdatedSubscriber.ProcessUserEvent(userEventMessage, deliveryCount, _consoleLogger);

            // Assert
            Assert.Equal(settlementAction, processResult.settlementAction);
            Assert.Equal(eventId, processResult.eventId);
            Assert.Equal(message, processResult.message);
        }

        private Message CreateServiceBusMessage(string body)
        {
            // Create Service Bus message
            var messageBody = Encoding.UTF8.GetBytes(body);
            var userEvent =JsonSerializer.Deserialize<UserEventDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var userEventMessage = new Message(messageBody)
            {
                MessageId = $"{userEvent.Id}"
            };

            // Add user properties to the Service Bus message
            userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.TraceId.ToString(), Guid.NewGuid().ToString()); ;
            userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.BatchId.ToString(), Guid.NewGuid().ToString());
            userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.EntityId.ToString(), userEvent.Id.ToString());
            userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.Source.ToString(), "ClientApp");
            userEventMessage.UserProperties.Add(ServiceBusConstants.MessageUserProperties.Timestamp.ToString(), userEvent.Timestamp.ToString("o"));

            return userEventMessage;
        }
    }
}
