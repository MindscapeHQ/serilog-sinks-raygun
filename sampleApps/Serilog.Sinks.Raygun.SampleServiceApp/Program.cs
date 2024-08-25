using Mindscape.Raygun4Net;
using Serilog;
using Serilog.Sinks.Raygun.Extensions;
using Serilog.Sinks.Raygun.SampleServiceApp;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddRaygun(context.Configuration);
        services.AddHostedService<Worker>();
    })
    .UseSerilog((_, serviceProvider, config) =>
    {
        config.WriteTo.Raygun(serviceProvider.GetRequiredService<RaygunClient>());
    })
    .Build();

await host.RunAsync();