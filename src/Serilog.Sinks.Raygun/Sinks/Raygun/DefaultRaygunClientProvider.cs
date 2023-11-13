using Mindscape.Raygun4Net;

namespace Serilog.Sinks.Raygun;

internal sealed class DefaultRaygunClientProvider : IRaygunClientProvider
{
    public RaygunClient GetClient(RaygunSettings settings)
    {
#if NETFRAMEWORK
        return new RaygunClient(settings.ApiKey);
#else
        return new RaygunClient(settings);
#endif
    }
}