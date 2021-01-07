using Integration.Observability.PubSub.Functions;
using System;
using System.Threading.Tasks;
using UnitTests.Integration.Observability.PubSub.FnApp.Helpers;
using Xunit;

namespace UnitTests.Integration.Observability.PubSub.FnApp
{
    public class UserUpdatedPublisherTests
    {
        [Theory]
        [InlineData("UserEventInvalidEmpty.json")]
        [InlineData("UserEventInvalidDate.json")]
        [InlineData("UserEventInvalidEmptyArray.json")]
        public void When_InvalidPayload_ReceiveFalse(string payloadFileName)
        {
            //Arrange
            var payload = TestDataHelper.GetTestDataStringFromFile("UserEvents", payloadFileName);
            var userUpdatedPublisher = new UserUpdatedPublisher();

            // Act
            var isValid = userUpdatedPublisher.TrySerialiseUserEvents(payload, out var cloudEvent);

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("UserEventValid01.json")]
        public void When_ValidPayload_ReceiveTrue(string payloadFileName)
        {
            //Arrange
            var payload = TestDataHelper.GetTestDataStringFromFile("UserEvents", payloadFileName);
            var userUpdatedPublisher = new UserUpdatedPublisher();

            // Act
            var isValid = userUpdatedPublisher.TrySerialiseUserEvents(payload, out var cloudEvent);

            // Assert
            Assert.True(isValid);
        }
    }
}
