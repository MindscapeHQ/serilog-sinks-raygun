#if !NETFRAMEWORK
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Mindscape.Raygun4Net;
using NUnit.Framework;
using RaygunSinkRegressionTests;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Raygun.Tests.Sinks.Raygun
{
    [TestFixture]
    public class StackTraceTests
    {
        private const string ApiKey = "test-api-key";

        [TestCase(false, TestName = "RaygunSink_MessageOnlyLogIncludesSourceFileAndLine")]
        [TestCase(true, TestName = "RaygunClientSink_MessageOnlyLogIncludesSourceFileAndLine")]
        public void MessageOnlyLogIncludesSourceFileAndLine(bool useRaygunClientSink)
        {
            var client = new RaygunClient(new RaygunSettings { ApiKey = ApiKey });
            RaygunMessage capturedMessage = null;
            client.SendingMessage += (sender, arguments) =>
            {
                arguments.Cancel = true;
                capturedMessage = arguments.Message;
            };

            ILogEventSink sink;
            if (useRaygunClientSink)
            {
                sink = new RaygunClientSink(client);
            }
            else
            {
                sink = new RaygunSink(
                    formatProvider: null,
                    applicationKey: ApiKey,
                    raygunClientProvider: new FixedRaygunClientProvider(client));
            }

            var sourceLocation = ApplicationLogCaller.Emit(sink);

            Assert.IsNotNull(capturedMessage, "The sink should build a message before the send is canceled.");
            var error = capturedMessage.Details.Error;
            Assert.AreEqual(ApplicationLogCaller.Message, error.ClassName);
            Assert.AreEqual(ApplicationLogCaller.Message, error.Message);
            Assert.IsNotEmpty(error.StackTrace);
            Assert.AreEqual(typeof(ApplicationLogCaller).FullName, error.StackTrace[0].ClassName,
                "The stack should start at the application caller after trimming Serilog frames.");

            var callerFrame = error.StackTrace.Single(frame =>
                frame.ClassName == typeof(StackTraceTests).FullName &&
                frame.MethodName.StartsWith(nameof(MessageOnlyLogIncludesSourceFileAndLine), StringComparison.Ordinal));

            Assert.Multiple(() =>
            {
                Assert.AreEqual(sourceLocation.FileName, callerFrame.FileName);
                Assert.AreEqual(sourceLocation.LineNumber, callerFrame.LineNumber);
                Assert.AreEqual(sourceLocation.FileName, error.StackTrace[0].FileName);
                Assert.Greater(error.StackTrace[0].LineNumber, 0);
            });
        }

        private sealed class FixedRaygunClientProvider : IRaygunClientProvider
        {
            private readonly RaygunClient _client;

            public FixedRaygunClientProvider(RaygunClient client)
            {
                _client = client;
            }

            public RaygunClient GetClient(RaygunSettings settings)
            {
                return _client;
            }
        }
    }
}

// Use an application namespace so the sink does not trim this logging frame as Serilog code.
namespace RaygunSinkRegressionTests
{
    internal static class ApplicationLogCaller
    {
        internal const string Message = "Message-only failure";

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static SourceLocation Emit(ILogEventSink sink,
            [CallerFilePath] string fileName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            // Fatal makes Emit synchronous; SendingMessage cancels the send before any network request.
            sink.Emit(new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Fatal,
                null,
                new MessageTemplateParser().Parse(Message),
                Enumerable.Empty<LogEventProperty>()));

            return new SourceLocation(fileName, lineNumber);
        }
    }

    internal sealed class SourceLocation
    {
        public SourceLocation(string fileName, int lineNumber)
        {
            FileName = fileName;
            LineNumber = lineNumber;
        }

        public string FileName { get; }
        public int LineNumber { get; }
    }
}
#endif
