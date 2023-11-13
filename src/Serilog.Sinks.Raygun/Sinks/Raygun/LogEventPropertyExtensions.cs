using System.Collections;
using System.Linq;
using Serilog.Events;

namespace Serilog.Sinks.Raygun;

public static class LogEventPropertyExtensions
{
    public static string AsString(this LogEventPropertyValue propertyValue)
    {
        if (!(propertyValue is ScalarValue scalar))
        {
            return null;
        }

        // Handle string values differently as the ToString() method will wrap the string in unwanted quotes
        return scalar.Value is string s ? s : scalar.ToString();
    }

    public static string AsString(this LogEventProperty property)
    {
        return property.Value.AsString();
    }

    public static int AsInteger(this LogEventProperty property, int defaultIfNull = 0)
    {
        var scalar = property.Value as ScalarValue;

        if (scalar?.Value == null)
        {
            return defaultIfNull;
        }

        return int.TryParse(property.Value.AsString(), out int result) ? result : defaultIfNull;
    }

    public static IDictionary AsDictionary(this LogEventProperty property)
    {
        if (!(property.Value is DictionaryValue value))
        {
            return null;
        }

        return value.Elements.ToDictionary(
            kv => kv.Key.AsString(),
            kv => kv.Value is ScalarValue scalarValue ? scalarValue.Value : kv.Value);
    }
}