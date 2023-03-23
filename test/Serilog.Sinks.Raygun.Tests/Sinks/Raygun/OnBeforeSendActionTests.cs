using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Raygun.Tests.Sinks.Raygun
{
    [TestFixture]
    public class OnBeforeSendActionTests
    {
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
            
            raygunSink.Emit(new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()),
                new List<LogEventProperty>()));
            
            Assert.IsTrue(onBeforeSendFlag);
        }
    }
}