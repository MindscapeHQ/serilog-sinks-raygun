using System.Collections;
using System.Linq;
using Serilog.Events;

namespace Serilog.Sinks.Raygun
{
    public static class LogEventPropertyExtensions
    {
        public static string AsString(this LogEventProperty property)
        {
            var scalar = property.Value as ScalarValue;
            return scalar?.Value != null ? property.Value.ToString("l", null) : null;
        }

        public static int AsInteger(this LogEventProperty property, int defaultIfNull = 0)
        {
            var scalar = property.Value as ScalarValue;
            return scalar?.Value != null ? int.TryParse(property.Value.ToString(), out int result) ? result : defaultIfNull : defaultIfNull;
        }

        public static IDictionary AsDictionary(this LogEventProperty property)
        {
            if (!(property.Value is DictionaryValue value)) return null;

            return value.Elements.ToDictionary(
                kv => kv.Key.ToString("l", null),
                kv => (object)kv.Value);
        }
    }
}