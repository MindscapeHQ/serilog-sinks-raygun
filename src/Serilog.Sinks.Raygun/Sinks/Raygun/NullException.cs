using System;
using System.Diagnostics;

namespace Serilog.Sinks.Raygun.Sinks.Raygun
{
    // This is used to carry the code execution StackTrace from the Serilog Sink to the Raygun callback in the case that no exception has been provided.
    internal class NullException : Exception
    {
        private readonly StackTrace _stackTrace;

        public NullException(StackTrace stacktrace)
        {
            _stackTrace = stacktrace;
        }

        public StackTrace StackTrace
        {
            get { return _stackTrace; }
        }
    }
}
