## ASP.NET Core Enricher

With the release of v7.3.0, the ASP.NET Core Enricher has been removed from this package. 
This is because the enricher had a dependency on `Mindscape.Raygun4Net.AspNetCore`, which in turn had a dependency
on `Microsoft.AspNetCore.App`. Attempting to use `Serilog.Sinks.Raygun` in a MAUI app would result in an error.

If you still wish to use the enricher you can follow the steps outlined below.

### Create a new Enricher class

Note: You will need to include a dependency on [Mindscape.Raygun4Net.AspNetCore](https://www.nuget.org/packages/Mindscape.Raygun4Net.AspNetCore) for this to work.

```c#
public class RaygunClientHttpEnricher : ILogEventEnricher
{
    private const string RaygunRequestMessagePropertyName = "RaygunSink_RequestMessage";
    private const string RaygunResponseMessagePropertyName = "RaygunSink_ResponseMessage";

    readonly IHttpContextAccessor _httpContextAccessor;
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

        var options = new RaygunRequestMessageOptions
        {
            IsRawDataIgnored = _raygunSettings.IsRawDataIgnored,
            UseXmlRawDataFilter = _raygunSettings.UseXmlRawDataFilter,
            IsRawDataIgnoredWhenFilteringFailed = _raygunSettings.IsRawDataIgnoredWhenFilteringFailed,
            UseKeyValuePairRawDataFilter = _raygunSettings.UseKeyValuePairRawDataFilter
        };

        options.AddCookieNames(_raygunSettings.IgnoreCookieNames ?? Array.Empty<string>());
        options.AddHeaderNames(_raygunSettings.IgnoreHeaderNames ?? Array.Empty<string>());
        options.AddFormFieldNames(_raygunSettings.IgnoreFormFieldNames ?? Array.Empty<string>());
        options.AddQueryParameterNames(_raygunSettings.IgnoreQueryParameterNames ?? Array.Empty<string>());
        options.AddSensitiveFieldNames(_raygunSettings.IgnoreSensitiveFieldNames ?? Array.Empty<string>());
        options.AddServerVariableNames(_raygunSettings.IgnoreServerVariableNames ?? Array.Empty<string>());

        var httpRequestMessage = RaygunAspNetCoreRequestMessageBuilder
            .Build(_httpContextAccessor.HttpContext, options)
            .GetAwaiter()
            .GetResult();

        var httpResponseMessage = RaygunAspNetCoreResponseMessageBuilder.Build(_httpContextAccessor.HttpContext);

        // The Raygun request/response messages are stored in the logEvent properties collection.
        // When the error is sent to Raygun, these messages are extracted from the known properties
        // and then removed so as to not duplicate data in the payload.
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(RaygunRequestMessagePropertyName, httpRequestMessage, true));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(RaygunResponseMessagePropertyName, httpResponseMessage, true));
    }
}
```

### Include the enricher into Serilog configuration

Please note, this an example configuration to show the use of `Enrich.With(...)`
```c#
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.With(new RaygunClientHttpEnricher())
    .WriteTo.Console()
    .WriteTo.Raygun("*your api key*")
    .CreateLogger();
```

### Ensure HttpContextAccessor is registered with the DI container

When setting up the DI container, ensure that the `HttpContextAccessor` is registered.

```c#
services.AddHttpContextAccessor();
```

When errors are thrown they should now contain the original information from the Raygun Enricher.