using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Serilog.Events;

namespace Serilog.Sinks.Raygun.Tests.Sinks.Raygun
{
    [TestFixture]
    public class LogEventPropertyExtensionsTests
    {
        static object[] AsString_WithScalarValue_Cases =
        {
            new object[] { "test-value", "test-value" },
            new object[] { Guid.Parse("{1DF0D385-220D-49F0-A53E-717E3E313E7C}"), "1df0d385-220d-49f0-a53e-717e3e313e7c" },
            new object[] { 1337, "1337" },
            new object[] { 1337.7331, "1337.7331" },
            new object[] { null, "null" }
        };

        static object[] AsInteger_WithScalarValue_Cases =
        {
            new object[] { "-1", -1 },
            new object[] { "1337", 1337 },
            new object[] { "invalid", 0 },
            new object[] { null, 0 }
        };

        [TestCaseSource(nameof(AsString_WithScalarValue_Cases))]
        public void AsString_WithScalarValue_ReturnsExpectedString(object scalarValue, string expectedValue)
        {
            var logEventProperty = new LogEventProperty("test", new ScalarValue(scalarValue));
            string outputValue = logEventProperty.AsString();

            Assert.That(outputValue, Is.EqualTo(expectedValue));
        }

        [Test]
        public void AsString_WithSequenceValue_ReturnsNull()
        {
            var logEventProperty = new LogEventProperty("test", new SequenceValue(Array.Empty<LogEventPropertyValue>()));
            string outputValue = logEventProperty.AsString();

            Assert.That(outputValue, Is.EqualTo(null));
        }

        [TestCaseSource(nameof(AsInteger_WithScalarValue_Cases))]
        public void AsInteger_WithScalarValue_ReturnsExpectedInteger(object scalarValue, int expectedValue)
        {
            var logEventProperty = new LogEventProperty("test", new ScalarValue(scalarValue));
            int outputValue = logEventProperty.AsInteger();

            Assert.That(outputValue, Is.EqualTo(expectedValue));
        }

        [Test]
        public void AsInteger_WithSequenceValue_ReturnsDefaultValue()
        {
            var logEventProperty = new LogEventProperty("test", new SequenceValue(Array.Empty<LogEventPropertyValue>()));
            int outputValue = logEventProperty.AsInteger(99);

            Assert.That(outputValue, Is.EqualTo(99));
        }

        [Test]
        public void AsDictionary_WithDictionaryValue_ReturnsDictionaryWithCorrectValues()
        {
            var logEventProperty = new LogEventProperty("test", new DictionaryValue(new[]
            {
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("item1"), new ScalarValue("item1_value")),
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue("item2"), new ScalarValue("item2_value"))
            }));

            IDictionary outputValue = logEventProperty.AsDictionary();

            Assert.That(outputValue, Contains.Key("item1").WithValue("item1_value"));
            Assert.That(outputValue, Contains.Key("item2").WithValue("item2_value"));
        }

        [Test]
        public void AsDictionary_WithSequenceValue_ReturnsNull()
        {
            var logEventProperty = new LogEventProperty("test", new SequenceValue(Array.Empty<LogEventPropertyValue>()));
            IDictionary outputValue = logEventProperty.AsDictionary();

            Assert.That(outputValue, Is.EqualTo(null));
        }
    }
}