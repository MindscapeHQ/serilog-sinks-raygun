#if NET

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mindscape.Raygun4Net;

// ReSharper disable once CheckNamespace
namespace Serilog.Sinks.Raygun.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Registers the Raygun Client and Raygun Settings with the DI container. Settings will be fetched from the appsettings.json file,
    /// and can be overridden by providing a custom configuration delegate.
    /// </summary>
    public static IServiceCollection AddRaygun(this IServiceCollection services, IConfiguration configuration, Action<RaygunSettings>? options = null)
    {
        // Fetch settings from configuration or use default settings
        var settings = configuration.GetSection("RaygunSettings").Get<RaygunSettings>() ?? new RaygunSettings();
    
        // Override settings with user-provided settings
        options?.Invoke(settings);

        services.TryAddSingleton(settings);
        services.TryAddSingleton(s => new RaygunClient(s.GetService<RaygunSettings>()!, s.GetService<IRaygunUserProvider>()!));

        return services;
    }

    /// <summary>
    /// Registers the Raygun Client and Raygun Settings with the DI container. Settings will be defaulted and overridden by providing a custom configuration delegate.
    /// </summary>
    public static IServiceCollection AddRaygun(this IServiceCollection services, Action<RaygunSettings>? options)
    {
        // Since we are not using IConfiguration, we need to create a new instance of RaygunSettings
        var settings = new RaygunSettings();
    
        // Override settings with user-provided settings
        options?.Invoke(settings);
    
        services.TryAddSingleton(settings);
        services.TryAddSingleton(s => new RaygunClient(s.GetService<RaygunSettings>()!, s.GetService<IRaygunUserProvider>()!));

        return services;
    }
}

#endif