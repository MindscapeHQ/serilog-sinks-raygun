using Serilog;
using Serilog.Events;

Console.Out.WriteLine("Please enter your Raygun API key: ");
var raygunApiKey = Console.In.ReadLine();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Raygun(raygunApiKey,
        null,
        "CustomUserNameProperty",
        "CustomAppVersionProperty",
        LogEventLevel.Information,
        null,
        new[] { "globalTag1", "globalTag2" },
        new[] { "ignoreField1", "ignoreField2" },
        "CustomGroupKeyProperty",
        "CustomTagsProperty",
        "CustomUserInfoProperty",
        onBeforeSendArguments =>
        {
            Console.Out.WriteLine("OnBeforeSend called with the following arguments: " + onBeforeSendArguments);
            //Updating machine name
            onBeforeSendArguments.RaygunMessage.Details.MachineName = "Serilog.Sinks.Raygun.SampleConsoleApp Machine";
            
            //Testing Logging in Logger callback. Uncomment to cause a stack overflow
            //Log.Fatal("Recursive error");
            
            //Testing throwing an exception in Action
            throw new Exception("Exception thrown in callback action..");
            
        })
    .CreateLogger();

try
{
    throw new Exception("Serilog.Sinks.Raygun.SampleConsoleApp");
}
catch (Exception e)
{
    Console.Out.WriteLine("Sending message to Raygun");
    Log.Fatal(e, "Logging error");
}

throw new Exception("Unhandled Serilog.Sinks.Raygun.SampleConsoleApp exception");