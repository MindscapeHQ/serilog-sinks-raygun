using System;

#if (NET || NETSTANDARD2_0_OR_GREATER) && !NETFRAMEWORK
using Mindscape.Raygun4Net;
#else
using Mindscape.Raygun4Net.Messages;
#endif

namespace Serilog.Sinks.Raygun
{
    public readonly struct OnBeforeSendArguments
    {
        private readonly Exception _exception;
        private readonly RaygunMessage _raygunMessage;

        public Exception Exception => _exception;
        public RaygunMessage RaygunMessage => _raygunMessage;

        public OnBeforeSendArguments(Exception exception, RaygunMessage raygunMessage)
        {
            _exception = exception;
            _raygunMessage = raygunMessage;
        }
    }
}