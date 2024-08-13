using Mindscape.Raygun4Net.AspNetCore;
using Mindscape.Raygun4Net.AspNetCore.Builders;
using Serilog.Core;
using Serilog.Events;

public class RaygunClientHttpEnricher : ILogEventEnricher
{
    private const string RaygunRequestMessagePropertyName = "RaygunSink_RequestMessage";
    private const string RaygunResponseMessagePropertyName = "RaygunSink_ResponseMessage";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LogEventLevel _restrictedToMinimumLevel;
    private readonly RaygunSettings _raygunSettings; 

    public RaygunClientHttpEnricher(IHttpContextAccessor? httpContextAccessor = null, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Error, RaygunSettings? raygunSettings = null)
    {
        _httpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();
        _restrictedToMinimumLevel = restrictedToMinimumLevel;
        
        _raygunSettings = raygunSettings ?? new RaygunSettings();
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Level < _restrictedToMinimumLevel)
        {
            return;
        }

        if (_httpContextAccessor.HttpContext == null)
        {
            return;
        }

        var httpRequestMessage = RaygunAspNetCoreRequestMessageBuilder
            .Build(_httpContextAccessor.HttpContext, _raygunSettings)
            .GetAwaiter()
            .GetResult();

        var httpResponseMessage = RaygunAspNetCoreResponseMessageBuilder.Build(_httpContextAccessor.HttpContext);

        // The Raygun request/response messages are stored in the logEvent properties collection.
        // When the error is sent to Raygun, these messages are extracted from the known properties
        // and then removed in order to not duplicate data in the payload.
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(RaygunRequestMessagePropertyName, httpRequestMessage, true));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(RaygunResponseMessagePropertyName, httpResponseMessage, true));
    }
}