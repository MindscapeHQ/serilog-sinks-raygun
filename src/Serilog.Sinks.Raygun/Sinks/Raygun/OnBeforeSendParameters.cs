using System.Collections.Generic;
using Serilog.Events;

namespace Serilog.Sinks.Raygun
{
    public readonly struct OnBeforeSendParameters
    {
        private readonly LogEvent _logEvent;
        private readonly List<string> _tags;
        private readonly Dictionary<string, LogEventPropertyValue> _properties;

        public LogEvent LogEvent => _logEvent;
        public List<string> Tags => _tags;
        public Dictionary<string, LogEventPropertyValue> Properties => _properties;

        public OnBeforeSendParameters(LogEvent logEvent, List<string> tags, Dictionary<string, LogEventPropertyValue> properties)
        {
            _logEvent = logEvent;
            _tags = tags;
            _properties = properties;
        }
    }
}