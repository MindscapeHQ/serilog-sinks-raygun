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
There are two recommended options for initializing the Raygun Serilog Sink. Which option is best to use depends on the application. 

### Option 1 - Using logger configuration

You can initialize Raygun's Serilog Sink through a Logger Configuration. This should be done inside of the main entry point of your application - for instance, Program.Main(), Global.asax, Application_Start Etc. The exact entry point will differ between frameworks.

**Minimum setup example:**

```cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Raygun("paste_your_api_key_here")
    .CreateLogger();
```

**Example setup with optional properties:**
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Raygun("paste_your_api_key_here",
      ListOfWrapperExceptions,
      "CustomUserNameProperty",
      "CustomAppVersionProperty",
      LogEventLevel.Information,
      CustomFormatProvider,
      new[] { "globalTag1", "globalTag2" },
      new[] { "ignoreField1", "ignoreField2" },
      "CustomGroupKeyProperty",
      "CustomTagsProperty",
      "CustomUserInfoProperty",
      onBeforeSendArguments => { /*OnBeforeSend: Action<onBeforeSendArguments>*/ })
    .CreateLogger();
```
------

### Option 2 - Using the JSON configuration file
You can initialize Raygun's Serilog Sink inside a Serilog JSON configuration file using the following examples.

**Minimum setup example:**

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Raygun"
    ],
    "WriteTo": [
      {
        "Name": "Raygun",
        "Args": {
          "applicationKey": "paste_your_api_key_here"
		}
      }
    ]
  }  
}
```

**Example setup with optional properties:**
```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Raygun"
    ],
    "WriteTo": [
      {
        "Name": "Raygun",
        "Args": {
          "applicationKey": "paste_your_api_key_here",
          "userNameProperty": "CustomUserNameProperty",
          "applicationVersionProperty": "CustomAppVersionProperty",
          "restrictedToMinimumLevel": "Error",
          "ignoredFormFieldNames": ["ignoreField1", "ignoreField2"],
          "tags": ["globalTag1", "globalTag2"],
          "groupKeyProperty": "CustomGroupKeyProperty",
          "tagsProperty": "CustomTagsProperty",
          "userInfoProperty": "CustomUserInfoProperty"
        }
      }
    ]
  }  
}
```

----

## Configuration Properties

**applicationKey**

`type: string`

`required`

Each application you create in Raygun will have an API key which you can pass in here to specify where the crash reports will be sent to. Although this is required, you can set this to null or empty string which would result in crash reports not being sent. This can be useful if you want to configure your local environment to not send crash reports to Raygun and then use config transforms or the like to provide an API key for other environments.

**wrapperExceptions**

`type: IEnumerable<Type>`

`default: null`

This is a list of wrapper exception types that you're not interested in logging to Raygun. Whenever an undesired wrapper exception is logged, it will be discarded and only the inner exception(s) will be logged.

For example, you may not be interested in the details of an AggregateException, so you could include `typeof(AggregateException)` in this list of wrapperExceptions. All inner exceptions of any logged AggregateException would then be sent to Raygun as separate crash reports.


**userNameProperty**

`type: string`

`default: UserName`

This is so you can specify the username to log the information as a part of the crash report. By default it is UserName. You can set this to be to `null` if you do not want to use this feature.

```cs
Log.ForContext("CustomUserNameProperty", "John Doe").Error(new Exception("random error"), "other information");
```


**applicationVersionProperty**

`type: string`

`default: ApplicationVersion`

By default, crash reports sent to Raygun will have an ApplicationVersion field based on the version of the entry assembly for your application. If this is not being picked up correctly, or if you want to provide a different version, then you can do so by including the desired value in the logging properties collection.

You can specify the property key that you place the version in by using this `applicationVersionProperty` setting. Otherwise the version will be read from the "ApplicationVersion" key.

```cs
Log.ForContext("CustomAppVersionProperty", "1.2.11").Error(new Exception("random error"), "other information");
```

**restrictedToMinimumLevel**

`type: LogEventLevel`

`default: LogEventLevel.Error`

You can set the minimum log event level required in order to write an event to the sink. By default, this is set to Error as Raygun is mostly used for error reporting.

**formatProvider**

`type: IFormatProvider`

`default: null`

This property supplies culture-specific formatting information. By default, it is null.

**tags**

`type: IEnumerable<string>`

`default: null`

This is a list of global tags that will be included on every crash report sent with this Serilog sink.


**ignoredFormFieldNames**

`type: IEnumerable<string>`

`default: null`

Crash reports sent to Raygun from this Serilog sink will include HTTP context details if present. (Currently only supported in .NET Framework applications). This option lets you specify a list of form fields that you do not want to be sent to Raygun.

Setting `ignoredFormFieldNames` to a list that only contains "*" will cause no form fields to be sent to Raygun. Placing * before, after or at both ends of an entry will perform an ends-with, starts-with or contains operation respectively.

Note that HTTP headers, query parameters, cookies, server variables and raw request data can also be filtered out. Configuration to do so is described in the [RaygunSettings](#raygun4net-features-configured-via-raygunsettings) section further below.

The `ignoreFormFieldNames` entries will also strip out specified values from the raw request payload if it is multipart/form-data.


**groupKeyProperty**

`type: string`

`default: GroupKey`

Crash reports sent to Raygun will be automatically grouped together based on stack trace and exception type information. The `groupKeyProperty` setting specifies a key in the logging properties collection where you can provide a grouping key. Crash reports containing a grouping key will not be grouped automatically by Raygun. Instead, crash reports with matching custom grouping keys will be grouped together.

```cs
Log.ForContext("CustomGroupKeyProperty", "TransactionId-12345").Error(new Exception("random error"), "other information");
```


**tagsProperty**

`type: string`

`default: Tags`

This allows you to specify a key in the properties collection that contains a list of tags to include on crash reports. Note that these will be included in addition to any global tags [described above](#tags). If you set a list of tags in the properties collection multiple times (e.g. at different logging scopes) then only the latest list of tags will be used.

```cs
Log.ForContext("CustomTagsProperty", new[] {"tag1", "tag2"}).Error(new Exception("random error"), "other information");
Log.Error(new Exception("random error"), "other information {@CustomTagsProperty}", new[] {"tag3", "tag4"});
```

**userInfoProperty**

`type: string`

`default: null`

This is null by default, so you need to configure the `userInfoProperty` name if you want to log more user information in this way. This will cause the provided RaygunIdentifierMessage to be included in the "User" section of the Raygun payload, allowing the information to be picked up by the "Users" section of the Raygun service. It's recommended to destructure the RaygunIdentifierMessage, but this feature will still work if you don't. Sending user information in this way will overwrite the use of the `userNameProperty`.

The user identifier passed into the RaygunIdentifierMessage constructor could be the users name, email address, database id or whatever works best for you to identify unique users.

```cs
var userInfo = new RaygunIdentifierMessage("12345")
{
    FirstName = "John",
    FullName = "John Doe",
    Email = "johndoe@email.address"
};

Log.ForContext("CustomUserInfoProperty", userInfo, true).Error(new Exception("random error"), "other information");
```


**onBeforeSend**

`type: Action<OnBeforeSendParameters>`

`default: null`

This action allows you to manipulate the crash report payloads that get sent to Raygun. By default it is `null`, so you don't need to set it in the constructor. If the action is `null`, nothing happens; if an `Action<OnBeforeSendParameters>` is passed, it gets called just before the crash report payload gets serialized and sent to Raygun. The arguments to the action are of type `Struct OnBeforeSendArguments`; they are passed to the action when it is called and contain references to the following objects passed by the Raygun client object:

```cs
// Abstracted away version of the struct to just show the properties
struct OnBeforeSendArguments
{
    System.Exception Exception;
    Mindscape.Raygun4Net.Messages.RaygunMessage RaygunMessage;
}
```

The provided action can read and/or modify their properties accordingly to produce the desired effect. For example, one can change the `MachineName` property in the `Details` of the `RaygunMessage` as follows:

 ```cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Raygun(
        applicationKey: "paste_your_api_key_here",
        onBeforeSend: arguments => { 
            arguments.RaygunMessage.Details.MachineName = "MyMachine";
        })
    .CreateLogger();
```

------

## Enrich with HTTP request and response data

This is only valid for .NET Standard 2.0 and above projects. 

In full framework, ASP.NET applications, the HTTP request and response are available to Raygun4Net through the `HttpContext.Current` accessor. 

For .NET Core, this won't be avaliable. Therefore, you'll need to add the Serilog enricher using the `WithHttpDataForRaygun` method to capture the HTTP request and response data.


### Configuration

All parameters to WithHttpDataForRaygun are optional.

```cs
Log.Logger = new LoggerConfiguration()
  .WriteTo.Raygun("paste_your_api_key_here")
  .Enrich.WithHttpDataForRaygun(
    new HttpContextAccessor(),
    LogEventLevel.Error,
    RaygunSettings)
  .CreateLogger();
```

When configuring using a JSON configuration file use the following example.

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Raygun"
    ],
    "Enrich": [
      {
        "Name": "WithHttpDataForRaygun",
        "Args": {
          "RaygunSettings": {
            "IsRawDataIgnored": true,
            "UseXmlRawDataFilter": true,
            "IsRawDataIgnoredWhenFilteringFailed": true,
            "UseKeyValuePairRawDataFilter": true,
            "IgnoreCookieNames": ["CookieName"],
            "IgnoreHeaderNames": ["HeaderName"],
            "IgnoreFormFieldNames": ["FormFieldName"],
            "IgnoreQueryParameterNames": ["QueryParameterName"],
            "IgnoreSensitiveFieldNames": ["SensitiveFieldNames"],
            "IgnoreServerVariableNames": ["ServerVariableName"]
          }
        }
      }
    ]
  }
}
```

------

## Raygun4Net features configured via RaygunSettings

This sink wraps the [Raygun4Net](https://github.com/MindscapeHQ/raygun4net) provider to build a crash report from an Exception and send it to Raygun. This makes the following Raygun4Net features available to you. To use these features, you need to add RaygunSettings to your configuration as explained below which is separate to the Serilog configuration.


**.NET Framework**

Add the following section within the configSections element of your app.config or web.config file.

```xml
<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>
```

Then add a RaygunSettings element containing the desired settings somewhere within the configuration element of the app.config or web.config file.

```xml
<RaygunSettings setting="value"/>
```

**ThrowOnError**

`type: bool`

`default: false`

This is false by default. Which means that any exception that occur within Raygun4Net itself will be silently caught. Setting this to true will allow any exceptions occurring in Raygun4Net to be thrown, which can help debug issues with Raygun4Net if crash reports aren't showing up in Raygun.


**IgnoreSensitiveFieldNames**

`type: string[]`

`default: null`

Crash reports sent to Raygun from this Serilog sink will include HTTP context details if present. (Currently only supported in .NET Framework applications). `IgnoreSensitiveFieldNames` lets you specify a list of HTTP query parameters, form fields, headers, cookies and server variables that you do not want to be sent to Raygun. Additionally, entries in this setting will be attempted to be stripped out of the raw request payload (more options for controlling this are explained in the `IsRawDataIgnored` section below).

Setting `IgnoreSensitiveFieldNames` to a list that only contains "*" will cause none of these things to be sent to Raygun. Placing `*` before, after or at both ends of an entry will perform an ends-with, starts-with or contains operation respectively.

Individual options are also available which function in the same way as `IgnoreSensitiveFieldNames`: `IgnoreQueryParameterNames`, `IgnoreFormFieldNames`, `IgnoreHeaderNames`, `IgnoreCookieNames` and `IgnoreServerVariableNames`.

The `IgnoreFormFieldNames` entries will also strip out specified values from the raw request payload if it is multipart/form-data.


**IsRawDataIgnored**

`type: bool`

`default: false`

By default, Raygun crash reports will capture the raw request payload of the current HTTP context if present. (Currently only supported in .NET Framework applications). If you would not like to include raw request payloads on crash reports sent to Raygun, then you can set `IsRawDataIgnored` to true.

If you do want to include the raw request payload, but want to filter out sensitive fields, then you can use the `IgnoreSensitiveFieldNames` options described above. You'll also need to specify how the fields should be stripped from the raw request payload. Set `UseXmlRawDataFilter` to true for XML payloads or/and set `UseKeyValuePairRawDataFilter` to true for payloads of the format "key1=value1&key2=value2".

Setting `IsRawDataIgnoredWhenFilteringFailed` to true will cause the entire raw request payload to be ignored in cases where specified sensitive values fail to be stripped out.


**CrashReportingOfflineStorageEnabled**

`type: bool`

`default: true`

Only available in .NET Framework applications. This is true by default which will cause crash reports to be saved to isolated storage (if possible) in cases where they fail to be sent to Raygun. This option lets you disable this functionality by setting it to false. When enabled, a maximum of 64 crash reports can be saved. This limit can be set lower than 64 via the `MaxCrashReportsStoredOffline` option.
