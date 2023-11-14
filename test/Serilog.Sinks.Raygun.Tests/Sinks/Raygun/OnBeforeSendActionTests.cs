using System;
using System.Collections.Generic;
using System.Linq;
using Mindscape.Raygun4Net;
#if NETFRAMEWORK
using Mindscape.Raygun4Net.Messages;
#endif
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Raygun.Tests.Sinks.Raygun
{
    [TestFixture]
    public class OnBeforeSendActionTests
    {
        [Test]
        public void TestCreatingAndChangingOnBeforeSendArguments()
        {
            var exception = new Exception();
            var raygunMessage = new RaygunMessage();
            
            var onBeforeSendArguments = new OnBeforeSendArguments(
                exception: exception,
                raygunMessage: raygunMessage
            );
            
            Assert.IsNotNull(onBeforeSendArguments);

            onBeforeSendArguments.RaygunMessage.Details.MachineName = "TestMachineName";
            Assert.AreEqual("TestMachineName", onBeforeSendArguments.RaygunMessage.Details.MachineName);
        }
        
        [Test]
        public void TestCreatingRaygunSinkWithOnBeforeSendAction()
        {
            var raygunSink = new RaygunSink(
                formatProvider: null,
                applicationKey: "",
                onBeforeSend: arguments => { }
                );
            
            Assert.NotNull(raygunSink);
        }
        
        [Test]
        public void TestCreatingLoggerConfigurationWithOnBeforeSendActionViaExtensionMethod()
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Raygun(
                    applicationKey: "",
                    onBeforeSend: arguments => { }
                    )
                .CreateLogger();
        
            Assert.NotNull(logger);
        }

        [Test]
        public void TestOnBeforeSendActionCalledAfterEmit()
        {
            var onBeforeSendFlag = false;
            var raygunSink = new RaygunSink(
                formatProvider: null,
                applicationKey: "",
                onBeforeSend: arguments =>
                {
                    onBeforeSendFlag = true;
                }
            );
            
            Assert.NotNull(raygunSink);
            Assert.IsFalse(onBeforeSendFlag);

            try
            {
                throw new Exception("TestOnBeforeSendActionCalledAfterEmit Exception");
            }
            catch (Exception e)
            {
                raygunSink.Emit(new LogEvent(
                    DateTimeOffset.Now,
                    LogEventLevel.Fatal, /*Force synchronous send for easy testing*/
                    e,
                    new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()),
                    new List<LogEventProperty>()));
            }

            Assert.IsTrue(onBeforeSendFlag);
        }

        [Test]
        public void TestOnBeforeActionCanChangeMachineName()
        {
            RaygunMessage raygunMessage = null;
            var raygunSink = new RaygunSink(
                formatProvider: null,
                applicationKey: "",
                onBeforeSend: arguments =>
                {
                    raygunMessage = arguments.RaygunMessage;
                    arguments.RaygunMessage.Details.MachineName = "TestMachineName";
                }
            );
            
            try
            {
                throw new Exception("TestOnBeforeActionCanChangeMachineName Exception");
            }
            catch (Exception e)
            {
                raygunSink.Emit(new LogEvent(
                    DateTimeOffset.Now,
                    LogEventLevel.Fatal, /*Force synchronous send for easy testing*/
                    e,
                    new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()),
                    new List<LogEventProperty>()));
            }
            
            Assert.NotNull(raygunMessage);
            Assert.AreEqual("TestMachineName", raygunMessage.Details.MachineName);
        }
    }
}