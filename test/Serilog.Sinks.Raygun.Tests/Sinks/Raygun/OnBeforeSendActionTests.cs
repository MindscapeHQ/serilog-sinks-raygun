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
        public void TestCreatingAndChangingOnBeforeSendParameters()
        {
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Debug,
                new Exception("Test Exception"),
                new MessageTemplate(new TextToken[] { }),
                new LogEventProperty[] { }
            );

            var tags = new List<string>();
            var properties = new Dictionary<string, LogEventPropertyValue>();
            
            var onBeforeSendParameters = new OnBeforeSendParameters(
                logEvent: logEvent,
                tags: tags,
                properties: properties
            );
            
            Assert.IsNotNull(onBeforeSendParameters);
            
            onBeforeSendParameters.Tags.Add("testTag");
            Assert.IsTrue(onBeforeSendParameters.Tags.Contains("testTag"));
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