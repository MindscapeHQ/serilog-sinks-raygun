using System.Collections;
using System.Linq;
using Serilog.Events;

namespace Serilog.Sinks.Raygun
{
    public static class LogEventPropertyExtensions
    {
        public static string AsString(this LogEventPropertyValue propertyValue)
        {
            if (!(propertyValue is ScalarValue scalar)) return null;
            return scalar.Value is string s ? s : scalar.Value.ToString();
        }
        
        public static string AsString(this LogEventProperty property)
        {
            return property.Value.AsString();
        }

        public static int AsInteger(this LogEventProperty property, int defaultIfNull = 0)
        {
            var scalar = property.Value as ScalarValue;
            if (scalar?.Value == null) return defaultIfNull;
            return int.TryParse(property.Value.ToString(), out int result) ? result : defaultIfNull;
        }

        public static IDictionary AsDictionary(this LogEventProperty property)
        {
            if (!(property.Value is DictionaryValue value)) return null;

            return value.Elements.ToDictionary(
                kv => kv.Key.ToString("l", null),
                kv => kv.Value is ScalarValue scalarValue ? scalarValue.Value : kv.Value);
        }
    }
}