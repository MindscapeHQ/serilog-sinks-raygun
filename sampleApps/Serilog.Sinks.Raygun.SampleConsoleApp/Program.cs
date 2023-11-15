using Serilog;
using Serilog.Events;

Console.Out.WriteLine("Please enter your Raygun application key: ");
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
    Log.Error(e, "Logging error");
}

Log.CloseAndFlush();

//A Thread.Sleep is normally not necessary. Adding it here because the app is too small and there might not be enough time to send the errors to Raygun because of the asynchronous Send method used 
Thread.Sleep(1000); 

Console.Out.WriteLine("All done! Please check your Raygun App to ensure the two logged exceptions appear");
