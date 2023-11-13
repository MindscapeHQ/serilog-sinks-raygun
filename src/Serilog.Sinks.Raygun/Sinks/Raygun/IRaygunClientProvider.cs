using Mindscape.Raygun4Net;

namespace Serilog.Sinks.Raygun;

public interface IRaygunClientProvider
{
    RaygunClient GetClient(RaygunSettings settings);
}