using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.AspNetCore;

using Serilog;
using Serilog.Sinks.Raygun;
using RaygunClient = Mindscape.Raygun4Net.AspNetCore.RaygunClient;
using RaygunClientBase = Mindscape.Raygun4Net.RaygunClientBase;

var apiKey = Environment.GetEnvironmentVariable("RAYGUN_APIKEY") ?? "";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.With(new RaygunClientHttpEnricher())
    .WriteTo.Console()
    //.WriteTo.Raygun(apiKey)
    .CreateLogger();


try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddRaygun(builder.Configuration);
    builder.Services.AddRaygunUserProvider();

    builder.Host.UseSerilog((context, provider, config) =>
    {
        config.WriteTo.Raygun(raygunClient: provider.GetRequiredService<RaygunClient>());
    });

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
