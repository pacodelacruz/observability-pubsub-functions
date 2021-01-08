using Integration.Observability.PubSub.FnApp.Models;
using Integration.Observability.PubSub.FnApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using UnitTests.Integration.Observability.PubSub.FnApp.Helpers;
using Xunit;

namespace UnitTests.Integration.Observability.PubSub.FnApp
{
    public class UserUpdatedPublisherTests
    {
        private IOptions<FunctionOptions> _options;
        

        public UserUpdatedPublisherTests()
        {
            // Load configuration options from the app settings of the test project. 
            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", false)
               .Build();

            _options = Options.Create(configuration.GetSection("Values").Get<FunctionOptions>());
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
    }
}
