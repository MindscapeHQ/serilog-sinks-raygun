using System;

#if (NET || NETSTANDARD2_0_OR_GREATER) && !NETFRAMEWORK
using Mindscape.Raygun4Net;
#else
using Mindscape.Raygun4Net.Messages;
#endif

namespace Serilog.Sinks.Raygun;

public readonly struct OnBeforeSendArguments
{
    public Exception Exception { get; }

    public RaygunMessage RaygunMessage { get; }

    public OnBeforeSendArguments(Exception exception, RaygunMessage raygunMessage)
    {
        Exception = exception;
        RaygunMessage = raygunMessage;
    }
}