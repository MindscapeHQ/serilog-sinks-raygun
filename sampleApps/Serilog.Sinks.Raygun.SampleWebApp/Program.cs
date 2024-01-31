using Mindscape.Raygun4Net.AspNetCore;
using Mindscape.Raygun4Net.AspNetCore.Builders;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using RaygunSettings = Mindscape.Raygun4Net.AspNetCore.RaygunSettings;

var apiKey = Environment.GetEnvironmentVariable("RAYGUN_APIKEY") ?? "";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.With(new RaygunClientHttpEnricher())
    .WriteTo.Console()
    .WriteTo.Raygun(apiKey)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddHttpContextAccessor();

    builder.Host.UseSerilog();

    var app = builder.Build();


    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception e)
{
    Log.Error(e, "Logging error");
}
finally
{
    Log.CloseAndFlush();
}


public class RaygunClientHttpEnricher : ILogEventEnricher
{
    private const string RaygunRequestMessagePropertyName = "RaygunSink_RequestMessage";
    private const string RaygunResponseMessagePropertyName = "RaygunSink_ResponseMessage";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LogEventLevel _restrictedToMinimumLevel;
    private readonly RaygunRequestMessageOptions _messageOptions; 

    public RaygunClientHttpEnricher(IHttpContextAccessor? httpContextAccessor = null, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Error, RaygunSettings? raygunSettings = null)
    {
        _httpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();
        _restrictedToMinimumLevel = restrictedToMinimumLevel;
        
        var settings = raygunSettings ?? new RaygunSettings();
        
        _messageOptions = new RaygunRequestMessageOptions
        {
            IsRawDataIgnored = settings.IsRawDataIgnored,
            UseXmlRawDataFilter = settings.UseXmlRawDataFilter,
            IsRawDataIgnoredWhenFilteringFailed = settings.IsRawDataIgnoredWhenFilteringFailed,
            UseKeyValuePairRawDataFilter = settings.UseKeyValuePairRawDataFilter
        };

        _messageOptions.AddCookieNames(settings.IgnoreCookieNames ?? Array.Empty<string>());
        _messageOptions.AddHeaderNames(settings.IgnoreHeaderNames ?? Array.Empty<string>());
        _messageOptions.AddFormFieldNames(settings.IgnoreFormFieldNames ?? Array.Empty<string>());
        _messageOptions.AddQueryParameterNames(settings.IgnoreQueryParameterNames ?? Array.Empty<string>());
        _messageOptions.AddSensitiveFieldNames(settings.IgnoreSensitiveFieldNames ?? Array.Empty<string>());
        _messageOptions.AddServerVariableNames(settings.IgnoreServerVariableNames ?? Array.Empty<string>());
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
            .Build(_httpContextAccessor.HttpContext, _messageOptions)
            .GetAwaiter()
            .GetResult();

        var httpResponseMessage = RaygunAspNetCoreResponseMessageBuilder.Build(_httpContextAccessor.HttpContext);

        // The Raygun request/response messages are stored in the logEvent properties collection.
        // When the error is sent to Raygun, these messages are extracted from the known properties
        // and then removed so as to not duplicate data in the payload.
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(RaygunRequestMessagePropertyName, httpRequestMessage, true));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(RaygunResponseMessagePropertyName, httpResponseMessage, true));
    }
}