using System;
using System.Collections.Generic;
using System.Linq;
using Mindscape.Raygun4Net;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Raygun.Tests.Sinks.Raygun
{
    [TestFixture]
    public class OnBeforeSendActionTests
    {
        [Test]
        public void TestCreatingAndChangingOnBeforeSendParameters()
        {
            var exception = new Exception();
            var raygunMessage = new RaygunMessage();
            
            var onBeforeSendParameters = new OnBeforeSendParameters(
                exception: exception,
                raygunMessage: raygunMessage
            );
            
            Assert.IsNotNull(onBeforeSendParameters);

            onBeforeSendParameters.RaygunMessage.Details.MachineName = "TestMachineName";
            Assert.AreEqual("TestMachineName", onBeforeSendParameters.RaygunMessage.Details.MachineName);
        }
        
        [Test]
        public void TestCreatingRaygunSinkWithOnBeforeSendAction()
        {
            var raygunSink = new RaygunSink(
                formatProvider: null,
                applicationKey: "",
                onBeforeSend: parameters => { }
                );
            
            Assert.NotNull(raygunSink);
        }

        [Test]
        public void TestOnBeforeSendActionCalledAfterEmit()
        {
            var onBeforeSendFlag = false;
            var raygunSink = new RaygunSink(
                formatProvider: null,
                applicationKey: "",
                onBeforeSend: parameters =>
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
                onBeforeSend: parameters =>
                {
                    raygunMessage = parameters.RaygunMessage;
                    parameters.RaygunMessage.Details.MachineName = "TestMachineName";
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