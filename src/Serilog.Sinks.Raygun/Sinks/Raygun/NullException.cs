using System;
using System.Diagnostics;

namespace Serilog.Sinks.Raygun;

// This is used to carry the code execution StackTrace from the Serilog Sink to the Raygun callback in the case that no exception has been provided.
internal class NullException : Exception
{
    public NullException(StackTrace stacktrace)
    {
        CodeExecutionStackTrace = stacktrace;
    }

    public StackTrace CodeExecutionStackTrace { get; }
}