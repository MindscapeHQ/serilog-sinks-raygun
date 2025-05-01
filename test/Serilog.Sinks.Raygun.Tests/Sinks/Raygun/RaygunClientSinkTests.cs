using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mindscape.Raygun4Net;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Raygun.Tests.Sinks.Raygun
{
#if !NETFRAMEWORK // Tests only apply to non-.NET Framework targets
    [TestFixture]
    public class RaygunClientSinkTests
    {
        // private Mock<RaygunClientBase> _mockRaygunClient; // Removed
        private LogEvent _logEvent = null!;
        // private RaygunMessage? _capturedMessage; // Removed

        // Define constants for property keys used internally by the sink
        private const string RenderedLogMessageProperty = "RenderedLogMessage";
        private const string LogMessageTemplateProperty = "LogMessageTemplate";
        private const string OccurredProperty = "RaygunSink_OccurredOn";
        private const string RaygunUserInfoPropertyName = "RaygunSink_UserInfo";

        [SetUp]
        public void Setup()
        {
            // _mockRaygunClient = new Mock<RaygunClientBase>(); // Removed
            // _capturedMessage = null; // Removed

            var messageTemplateParser = new MessageTemplateParser();
            _logEvent = new LogEvent(
                timestamp: DateTimeOffset.UtcNow,
                level: LogEventLevel.Information,
                exception: null,
                messageTemplate: messageTemplateParser.Parse("Test message"),
                properties: new List<LogEventProperty>());
        }

        // Helper to simulate property dictionary creation (simplified Emit)
        private IDictionary<string, object> GetPropertiesDictionary(LogEvent logEvent, Func<LogEvent, RaygunIdentifierMessage?>? userInfoCallback)
        {
            var properties = logEvent.Properties.ToDictionary(kv => kv.Key, kv => kv.Value as object);

            properties[RenderedLogMessageProperty] = logEvent.RenderMessage(null);
            properties[LogMessageTemplateProperty] = logEvent.MessageTemplate.Text;
            properties[OccurredProperty] = logEvent.Timestamp.UtcDateTime;

            if (userInfoCallback != null)
            {
                var userInfo = userInfoCallback(logEvent);
                if (userInfo != null)
                {
                    // Simulate how user info is stored before processing
                    var structureProps = new List<LogEventProperty>();
                    if (userInfo.Identifier != null) structureProps.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.Identifier), new ScalarValue(userInfo.Identifier)));
                    structureProps.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.IsAnonymous), new ScalarValue(userInfo.IsAnonymous)));
                    if (userInfo.Email != null) structureProps.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.Email), new ScalarValue(userInfo.Email)));
                    if (userInfo.FullName != null) structureProps.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.FullName), new ScalarValue(userInfo.FullName)));
                    if (userInfo.FirstName != null) structureProps.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.FirstName), new ScalarValue(userInfo.FirstName)));
                    if (userInfo.UUID != null) structureProps.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.UUID), new ScalarValue(userInfo.UUID)));
                    properties[RaygunUserInfoPropertyName] = new StructureValue(structureProps, nameof(RaygunIdentifierMessage));
                }
            }
            return properties;
        }

        [Test]
        public void UserInfoCallback_ProvidedAndReturnsUser_SetsUserDetails()
        {
            // Arrange
            var expectedUser = new RaygunIdentifierMessage("test-user-id")
            {
                Email = "test@example.com",
                FullName = "Test User",
                FirstName = "Test",
                UUID = "uuid-123",
                IsAnonymous = false
            };
            Func<LogEvent, RaygunIdentifierMessage?> callback = logEvent => expectedUser;
            var properties = GetPropertiesDictionary(_logEvent, callback);
            var details = new RaygunMessageDetails { UserCustomData = new Hashtable((IDictionary)properties) };
            var message = new RaygunMessage { Details = details };

            // Act
            RaygunClientSink.ProcessRaygunMessageDetails(message, _logEvent.Exception ?? new Exception("test"));

            // Assert
            Assert.IsNotNull(message.Details.User);
            Assert.AreEqual(expectedUser.Identifier, message.Details.User.Identifier);
            Assert.AreEqual(expectedUser.Email, message.Details.User.Email);
            Assert.AreEqual(expectedUser.FullName, message.Details.User.FullName);
            Assert.AreEqual(expectedUser.FirstName, message.Details.User.FirstName);
            Assert.AreEqual(expectedUser.UUID, message.Details.User.UUID);
            Assert.AreEqual(expectedUser.IsAnonymous, message.Details.User.IsAnonymous);
        }

        [Test]
        public void UserInfoCallback_ProvidedAndReturnsNull_DoesNotSetUserDetails()
        {
            // Arrange
            Func<LogEvent, RaygunIdentifierMessage?> callback = logEvent => null;
            var properties = GetPropertiesDictionary(_logEvent, callback);
            var details = new RaygunMessageDetails { UserCustomData = new Hashtable((IDictionary)properties) };
            var message = new RaygunMessage { Details = details };

            // Act
            RaygunClientSink.ProcessRaygunMessageDetails(message, _logEvent.Exception ?? new Exception("test"));

            // Assert
            Assert.IsNull(message.Details.User);
        }

        [Test]
        public void UserInfoCallback_NotProvided_DoesNotSetUserDetails()
        {
            // Arrange
            Func<LogEvent, RaygunIdentifierMessage?>? callback = null;
            var properties = GetPropertiesDictionary(_logEvent, callback);
            var details = new RaygunMessageDetails { UserCustomData = new Hashtable((IDictionary)properties) };
            var message = new RaygunMessage { Details = details };

            // Act
            RaygunClientSink.ProcessRaygunMessageDetails(message, _logEvent.Exception ?? new Exception("test"));

            // Assert
            Assert.IsNull(message.Details.User);
        }

        [Test]
        public void UserInfoCallback_Provided_RemovesTempProperty()
        {
            // Arrange
            Func<LogEvent, RaygunIdentifierMessage?> callback = logEvent => new RaygunIdentifierMessage("user");
            var properties = GetPropertiesDictionary(_logEvent, callback);
            var details = new RaygunMessageDetails { UserCustomData = new Hashtable((IDictionary)properties) };
            var message = new RaygunMessage { Details = details };
            Assert.IsTrue(message.Details.UserCustomData.Contains(RaygunUserInfoPropertyName), "Precondition: UserInfo property must exist before processing.");

            // Act
            RaygunClientSink.ProcessRaygunMessageDetails(message, _logEvent.Exception ?? new Exception("test"));

            // Assert
            Assert.IsNotNull(message.Details.UserCustomData);
            Assert.IsFalse(message.Details.UserCustomData.Contains(RaygunUserInfoPropertyName), $"Property '{RaygunUserInfoPropertyName}' should have been removed.");
        }
    }
#endif // !NETFRAMEWORK
}
