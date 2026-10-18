#if !NETFRAMEWORK
using System;
using System.Linq;
using System.Reflection;
using Mindscape.Raygun4Net;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Raygun.Tests.Sinks.Raygun
{
    [TestFixture]
    public class RaygunClientSinkTests
    {
        private RaygunClient _client;
        private RaygunMessage _capturedMessage;

        [SetUp]
        public void SetUp()
        {
            _client = new RaygunClient(new RaygunSettings { ApiKey = "test-api-key" });
            _capturedMessage = null;
            _client.SendingMessage += (sender, arguments) =>
            {
                arguments.Cancel = true;
                _capturedMessage = arguments.Message;
            };
        }

        [Test]
        public void SinkReportsUseSinkClientDetails()
        {
            var sink = new RaygunClientSink(_client);

            // Fatal makes Emit synchronous; SendingMessage cancels the send before any network request.
            sink.Emit(new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Fatal,
                new InvalidOperationException("sink"),
                new MessageTemplateParser().Parse("Sink failure"),
                Enumerable.Empty<LogEventProperty>()));

            Assert.AreEqual("RaygunSerilogSink", _capturedMessage.Details.Client.Name);
        }

        [Test]
        public void SinkReportsUnwrapTargetInvocationException()
        {
            var sink = new RaygunClientSink(_client);

            sink.Emit(new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Fatal,
                new TargetInvocationException(new InvalidOperationException("inner")),
                new MessageTemplateParser().Parse("Sink failure"),
                Enumerable.Empty<LogEventProperty>()));

            Assert.AreEqual(typeof(InvalidOperationException).FullName, _capturedMessage.Details.Error.ClassName);
            Assert.AreEqual("RaygunSerilogSink", _capturedMessage.Details.Client.Name);
        }

        [Test]
        public void OtherReportsKeepClientDetails()
        {
            _client.SendAsync(new InvalidOperationException("before"), null, null).GetAwaiter().GetResult();
            var expectedName = _capturedMessage.Details.Client.Name;

            _ = new RaygunClientSink(_client);
            _client.SendAsync(new InvalidOperationException("after"), null, null).GetAwaiter().GetResult();

            Assert.AreEqual(expectedName, _capturedMessage.Details.Client.Name);
        }

        [Test]
        public void OtherReportsKeepDefaultWrapperExceptions()
        {
            _ = new RaygunClientSink(_client);

            _client.SendAsync(new TargetInvocationException(new InvalidOperationException("inner")), null, null)
                .GetAwaiter().GetResult();

            Assert.AreEqual(typeof(InvalidOperationException).FullName, _capturedMessage.Details.Error.ClassName);
        }
    }
}
#endif
