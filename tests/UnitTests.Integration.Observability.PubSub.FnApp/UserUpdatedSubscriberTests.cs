using Integration.Observability.Constants;
using Integration.Observability.PubSub.FnApp;
using Integration.Observability.PubSub.FnApp.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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
            // Load configuration options from the appsettings.json file in the test project. 
            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", false)
               .Build();

            _options = Options.Create(configuration.GetSection("Values").Get<FunctionOptions>());
            _userUpdatedSubscriber = new UserUpdatedSubscriber(_options);

            // Send log messages to the output window during debug. 
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            _consoleLogger = loggerFactory.CreateLogger<UserUpdatedSubscriber>();
        }
           
        [Theory]
        [InlineData("UserEvent01ToComplete.json", 1, ServiceBusConstants.SettlementActions.Complete, LoggingConstants.Status.Succeeded, LoggingConstants.EventId.SubscriberDeliverySucceeded)]
        [InlineData("UserEvent01ToComplete.json", 2, ServiceBusConstants.SettlementActions.Complete, LoggingConstants.Status.Succeeded, LoggingConstants.EventId.SubscriberDeliverySucceeded)]
        [InlineData("UserEvent06ToUnreachableTarget.json", 1, ServiceBusConstants.SettlementActions.None, LoggingConstants.Status.AttemptFailed, LoggingConstants.EventId.SubscriberDeliveryUnreachableTarget)]
        [InlineData("UserEvent06ToUnreachableTarget.json", 2, ServiceBusConstants.SettlementActions.None, LoggingConstants.Status.Failed, LoggingConstants.EventId.SubscriberDeliveryUnreachableTarget)]
        [InlineData("UserEvent08SkipStaleMessage.json", 1, ServiceBusConstants.SettlementActions.Complete, LoggingConstants.Status.Skipped, LoggingConstants.EventId.SubscriberDeliverySkippedStaleMessage)]
        [InlineData("UserEvent08SkipStaleMessage.json", 2, ServiceBusConstants.SettlementActions.Complete, LoggingConstants.Status.Skipped, LoggingConstants.EventId.SubscriberDeliverySkippedStaleMessage)]
        [InlineData("UserEvent09InvalidMessage.json", 1, ServiceBusConstants.SettlementActions.DeadLetter, LoggingConstants.Status.Failed, LoggingConstants.EventId.SubscriberDeliveryFailedInvalidMessage)]
        [InlineData("UserEvent09InvalidMessage.json", 2, ServiceBusConstants.SettlementActions.DeadLetter, LoggingConstants.Status.Failed, LoggingConstants.EventId.SubscriberDeliveryFailedInvalidMessage)]

        public void When_ProcessUserEvent_ReturnExpectedResult(
            string payloadFileName, 
            int deliveryCount, 
            ServiceBusConstants.SettlementActions settlementAction,
            LoggingConstants.Status status,
            LoggingConstants.EventId eventId)
        {
            //Arrange
            var payload = TestDataHelper.GetTestDataStringFromFile("UserEvents", payloadFileName);
            var userEventMessage = CreateServiceBusMessage(payload);

            // Act
            var processResult = _userUpdatedSubscriber.ProcessUserEventSubscription(userEventMessage, deliveryCount, _consoleLogger);

            // Assert
            Assert.Equal(settlementAction, processResult.settlementAction);
            Assert.Equal(eventId, processResult.eventId);
            Assert.Equal(status, processResult.status);
        }

        public static IEnumerable<object[]> UserEventsWithPotentialResults =>
            new List<object[]>
            {
                new object[] {
                    "UserEvent07MissingDependency.json",
                    1, 
                    new List<ServiceBusConstants.SettlementActions> {ServiceBusConstants.SettlementActions.None, ServiceBusConstants.SettlementActions.Complete},
                    new List<LoggingConstants.Status> { LoggingConstants.Status.Succeeded, LoggingConstants.Status.AttemptFailed },
                    new List<LoggingConstants.EventId> { LoggingConstants.EventId.SubscriberDeliverySucceeded, LoggingConstants.EventId.SubscriberDeliveryFailedMissingDependency }
                },
                new object[] {
                    "UserEvent07MissingDependency.json",
                    2,
                    new List<ServiceBusConstants.SettlementActions> {ServiceBusConstants.SettlementActions.None, ServiceBusConstants.SettlementActions.Complete},
                    new List<LoggingConstants.Status> { LoggingConstants.Status.Succeeded, LoggingConstants.Status.Failed },
                    new List<LoggingConstants.EventId> { LoggingConstants.EventId.SubscriberDeliverySucceeded, LoggingConstants.EventId.SubscriberDeliveryFailedMissingDependency }
                },
            };

        [Theory]
        [MemberData(nameof(UserEventsWithPotentialResults))]
        public void When_ProcessUserEvent_ReturnPotentialExpectedResult(
            string payloadFileName,
            int deliveryCount,
            List<ServiceBusConstants.SettlementActions> expectedSettlementActions,
            List<LoggingConstants.Status> expectedStatuses,
            List<LoggingConstants.EventId> expectedEventIds)
        {
            //Arrange
            var payload = TestDataHelper.GetTestDataStringFromFile("UserEvents", payloadFileName);
            var userEventMessage = CreateServiceBusMessage(payload);

            // Act
            var processResult = _userUpdatedSubscriber.ProcessUserEventSubscription(userEventMessage, deliveryCount, _consoleLogger);

            // Assert
            Assert.Contains(processResult.settlementAction, expectedSettlementActions);
            Assert.Contains(processResult.status, expectedStatuses);
            Assert.Contains(processResult.eventId, expectedEventIds);
        }

        [Theory]
        [InlineData("UserEvent99Exception.json", 1)]
        public void When_ProcessUserEvent_ExpectException(
            string payloadFileName,
            int deliveryCount)
        {
            //Arrange
            var payload = TestDataHelper.GetTestDataStringFromFile("UserEvents", payloadFileName);
            var userEventMessage = CreateServiceBusMessage(payload);

            // Act & Assert
            var ex = Assert.Throws<ApplicationException>(() => _userUpdatedSubscriber.ProcessUserEventSubscription(userEventMessage, deliveryCount, _consoleLogger));
        }

        #region Private Methods

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

        #endregion
    }
}
