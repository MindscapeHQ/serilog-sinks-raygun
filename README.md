# serilog-sinks-raygun

Serilog Sinks error and performance monitoring with Raygun is available using the serilog-sinks-raygun provider.

serilog-sinks-raygun is a library that you can easily add to your website or web application, which will then monitor your application and display all Serilog errors and issues affecting your users within your Raygun account. Installation is painless.

The provider is a single package (Serilog.Sinks.Raygun) which includes the sole dependency (Serilog), allowing you to drop it straight in.

## Getting started

## Step 1 - Add packages

Install the Serilog (if not included already) and Serilog.Sinks.Raygun package into your project. You can either use the below dotnet CLI command, or the NuGet management GUI in the IDE you use.

```bash
 dotnet add package Serilog
 dotnet add package Serilog.Sinks.Raygun
```

------

## Step 2 - Initialization
The following examples are for .NET 6.0+ applications. For other frameworks, please refer to the [.NET Framework Readme](README-NET-FRAMEWORK.md).

### Example of setup for ASP.NET Applications:
```csharp
using Mindscape.Raygun4Net.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Raygun
builder.Services.AddRaygun(builder.Configuration);
builder.Services.AddRaygunUserProvider();

builder.Host.UseSerilog((context, provider, config) =>
{
    // Add the Raygun sink
    config.WriteTo.Raygun(raygunClient: provider.GetRequiredService<RaygunClient>());
});
```

### Example of setup for Console/Service:
```csharp
using Mindscape.Raygun4Net;
using Serilog;
using Serilog.Sinks.Raygun.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Add Raygun
        services.AddRaygun(context.Configuration);
        services.AddHostedService<Worker>();
    })
    .UseSerilog((_, serviceProvider, config) =>
    {
        // Add the Raygun sink
        config.WriteTo.Raygun(raygunClient: serviceProvider.GetRequiredService<RaygunClient>());
    })
    .Build();

await host.RunAsync();
```

### Example of setup for MAUI:
```csharp
using Serilog;

var builder = MauiApp.CreateBuilder();

builder
    .UseMauiApp<App>()

    // Add Raygun
    .AddRaygun();

var app = builder.Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()

    // Add the Raygun sink
    .WriteTo.Raygun(raygunClient: app.Services.GetRequiredService<RaygunMauiClient>())
    .CreateLogger();

return app;
```

----

## Configuration Properties

**raygunClient**

`type: RaygunClientBase`

`required`

This property is required for the Raygun Sink to function. The client can be any implementation that inherits from RaygunClientBase, this could be the Raygun4Maui client, Raygun4Net.AspNetCore client, or Raygun4Net.NetCore client. Ideally, this is resolved from the ServiceCollection in .NET Core applications.

**formatProvider**

`type: IFormatProvider`

`default: null`

This property supplies culture-specific formatting information. By default, it is null.

**restrictedToMinimumLevel**

`type: LogEventLevel`

`default: LogEventLevel.Error`

You can set the minimum log event level required in order to write an event to the sink. By default, this is set to Error as Raygun is mostly used for error reporting.


------

## Enrich with HTTP request and response data

Properties included from other Serilog Enrichers should automatically be included into the Raygun errors.

To use the old Raygun Enricher you can follow the [Enricher Readme](https://github.com/MindscapeHQ/serilog-sinks-raygun/blob/master/README-ENRICHER.md) to add the enricher to your project.

