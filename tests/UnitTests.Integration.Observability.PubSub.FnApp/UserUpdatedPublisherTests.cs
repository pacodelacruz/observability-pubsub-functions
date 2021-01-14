using Integration.Observability.PubSub.FnApp.Models;
using Integration.Observability.PubSub.FnApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using UnitTests.Integration.Observability.PubSub.FnApp.Helpers;
using Xunit;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http;
using Integration.Observability.Constants;
using Microsoft.AspNetCore.Mvc;

namespace UnitTests.Integration.Observability.PubSub.FnApp
{
    public class UserUpdatedPublisherTests
    {
        private IOptions<FunctionOptions> _options;
        private UserUpdatedPublisher _userUpdatedPublisher;
        private ILogger _consoleLogger;


        public UserUpdatedPublisherTests()
        {
            // Load configuration options from the app settings of the test project. 
            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", false)
               .Build();

            _options = Options.Create(configuration.GetSection("Values").Get<FunctionOptions>());

            _userUpdatedPublisher = new UserUpdatedPublisher(_options);

            // Send log messages to the output window during debug. 
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            _consoleLogger = loggerFactory.CreateLogger<UserUpdatedSubscriber>();
        }
            
        [Theory]
        [InlineData("UserEventInvalidEmpty.json")]
        [InlineData("UserEventInvalidDate.json")]
        [InlineData("UserEventInvalidEmptyArray.json")]
        public void When_InvalidPayload_ReceiveFalse(string payloadFileName)
        {
            //Arrange
            var payload = TestDataHelper.GetTestDataStringFromFile("UserEventsCloudEvents", payloadFileName);
            var userUpdatedPublisher = new UserUpdatedPublisher(_options);

            // Act
            var isValid = userUpdatedPublisher.TryDeserialiseUserEvents(payload, out var cloudEvent);

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("UserEventValid01.json")]
        public void When_ValidPayload_ReceiveTrue(string payloadFileName)
        {
            //Arrange
            var payload = TestDataHelper.GetTestDataStringFromFile("UserEventsCloudEvents", payloadFileName);
            var userUpdatedPublisher = new UserUpdatedPublisher(_options);

            // Act
            var isValid = userUpdatedPublisher.TryDeserialiseUserEvents(payload, out var cloudEvent);

            // Assert
            Assert.True(isValid);
        }

        [Theory]
        [InlineData("UserEventValid01.json", 2)]
        public void When_ValidPayloadRequest_ReceiveAccepted(string payloadFileName, int messageCount)
        {
            //Arrange
            var payload = TestDataHelper.GetTestDataStringFromFile("UserEventsCloudEvents", payloadFileName);
            var userUpdatedPublisher = new UserUpdatedPublisher(_options);

            // Act
            var processResult = userUpdatedPublisher.ProcessUserEventPublishing(payload, Guid.NewGuid().ToString(), _consoleLogger);
                
            //Assert
            var objectResult = processResult.requestResult as ObjectResult;

            Assert.NotNull(objectResult);
            Assert.Equal(StatusCodes.Status202Accepted, objectResult.StatusCode);
            Assert.Equal(messageCount, processResult.messages.Count);
        }

        [Theory]
        [InlineData("UserEventInvalidDate.json")]
        [InlineData("UserEventInvalidEmptyArray.json")]
        public void When_InValidPayloadRequest_ReceiveBadRequest(string payloadFileName)
        {
            //Arrange
            var payload = TestDataHelper.GetTestDataStringFromFile("UserEventsCloudEvents", payloadFileName);
            var userUpdatedPublisher = new UserUpdatedPublisher(_options);

            // Act
            var processResult = userUpdatedPublisher.ProcessUserEventPublishing(payload, Guid.NewGuid().ToString(), _consoleLogger);

            //Assert
            var objectResult = processResult.requestResult as ObjectResult;

            Assert.NotNull(objectResult);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }
    }
}
