# serilog-sinks-raygun

[![Build status](https://ci.appveyor.com/api/projects/status/bol0v48ujapxobym/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-raygun/branch/master)

A Serilog sink that writes events to Raygun

## Usage

Add the [Serilog](https://www.nuget.org/packages/serilog/) and [Serilog.Sinks.Raygun](https://www.nuget.org/packages/Serilog.Sinks.Raygun) nuget packages.
Via the package manager, or via the command line:

```powershell
 dotnet add package Serilog
 dotnet add package Serilog.Sinks.Raygun 
```

Then setup the logger configuration; inside the `Program.Main()`, `Global.asax.Application_Start`, etc.

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Raygun("RaygunAPIKey",
      ListOfWrapperExceptions,
      "CustomUserNameProperty",
      "CustomAppVersionProperty",
      LogEventLevel.Information,
      CustomFormatProvider,
      new[] { "globalTag1", "globalTag2" },
      new[] { "ignoreField1", "ignoreField2" },
      "CustomGroupKeyProperty",
      "CustomTagsProperty",
      "CustomUserInfoProperty")
    .CreateLogger();
```

When configuring using a JSON configuration file use the following example.

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
          "applicationKey": "RaygunAPIKey",
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

### applicationKey
`type: string`

`required`

Each application you create in Raygun will have an API Key which you can pass in here to specify where the crash reports will be sent to. Although this is required, you can set this to null or empty string which would result in crash reports not being sent. This can be useful if you want to configure your local environment to not send crash reports to Raygun and then use config transforms or the like to provide an API key for other environments.

### wrapperExceptions
`type: IEnumerable<Type>`

`default: null`

This is a list of wrapper exception types that you're not interested in logging to Raygun. Whenever an undesired wrapper exception is logged, it will be discarded and only the inner exception(s) will be logged.

For example, you may not be interested in the details of an AggregateException, so you could include `typeof(AggregateException)` in this list of wrapperExceptions. All inner exceptions of any logged AggregateException would then be sent to Raygun as separate crash reports.

### userNameProperty
`type: string`

`default: UserName`

```csharp
Log.ForContext("CustomUserNameProperty", "John Doe").Error(new Exception("random error"), "other information");
```

### applicationVersionProperty
`type: string`

`default: ApplicationVersion`

By default, crash reports sent to Raygun will have an ApplicationVersion field based on the version of the entry assembly for your application. If this is not being picked up correctly, or if you want to provide a different version, then you can do so by including the desired value in the logging properties collection.

You can specify the property key that you place the version in by using this applicationVersionProperty setting. Otherwise the version will be read from the "ApplicationVersion" key.

```csharp
Log.ForContext("CustomAppVersionProperty", "1.2.11").Error(new Exception("random error"), "other information");
```

### restrictedToMinimumLevel
`type: LogEventLevel`

`default: LogEventLevel.Error`

### formatProvider
`type: IFormatProvider`

`default: null`

### tags
`type: IEnumerable<string>`

`default: null`

This is a list of global tags that will be included on every crash report sent with this Serilog sink.

### ignoredFormFieldNames
`type: IEnumerable<string>`

`default: null`

Crash reports sent to Raygun from this Serilog sink will include HTTP context details if present. (Currently only supported in .NET Framework applications). This option lets you specify a list of form fields that you do not want to be sent to Raygun.

Setting `ignoredFormFieldNames` to a list that only contains "*" will cause no form fields to be sent to Raygun. Placing * before, after or at both ends of an entry will perform an ends-with, starts-with or contains operation respectively.

Note that HTTP headers, query parameters, cookies, server variables and raw request data can also be filtered out. Configuration to do so is described in the [RaygunSettings](#raygun4net-features-configured-via-raygunsettings) section further below.

The `ignoreFormFieldNames` entries will also strip out specified values from the raw request payload if it is multipart/form-data.

### groupKeyProperty
`type: string`

`default: GroupKey`

Crash reports sent to Raygun will be automatically grouped together based on stack trace and exception type information. The `groupKeyProperty` setting specifies a key in the logging properties collection where you can provide a grouping key. Crash reports containing a grouping key will not be grouped automatically by Raygun. Instead, crash reports with matching custom grouping keys will be grouped together.

```csharp
Log.ForContext("CustomGroupKeyProperty", "TransactionId-12345").Error(new Exception("random error"), "other information");
```

### tagsProperty
`type: string`

`default: Tags`

This allows you to specify a key in the properties collection that contains a list of tags to include on crash reports. Note that these will be included in addition to any global tags [described above](#tags). If you set a list of tags in the properties collection multiple times (e.g. at different logging scopes) then only the latest list of tags will be used.

```csharp
Log.ForContext("CustomTagsProperty", new[] {"tag1", "tag2"}).Error(new Exception("random error"), "other information");
Log.Error(new Exception("random error"), "other information {@CustomTagsProperty}", new[] {"tag3", "tag4"});
```

### userInfoProperty
`type: string`

`default: null`

This is null by default, so you need to configure the userInfoProperty name if you want to log more user information in this way. This will cause the provided RaygunIdentifierMessage to be included in the "User" section of the Raygun payload, allowing the information to be picked up by the "Users" section of the Raygun service. It's recommended to destructure the RaygunIdentifierMessage, but this feature will still work if you don't. Sending user information in this way will overwrite the use of the userNameProperty.

The user identifier passed into the RaygunIdentifierMessage constructor could be the users name, email address, database id or whatever works best for you to identify unique users.

```csharp
var userInfo = new RaygunIdentifierMessage("12345")
{
    FirstName = "John",
    FullName = "John Doe",
    Email = "johndoe@email.address"
};

Log.ForContext("CustomUserInfoProperty", userInfo, true).Error(new Exception("random error"), "other information");
```
## Enrich with HTTP request and response data

_Note: This is only valid for .NET Standard 2.0 and above projects. In full framework ASP.NET applications the HTTP request and response are available to Raygun4Net through the `HttpContext.Current` accessor.
In .NET Core this is not available so you'll need to add the Serilog enricher using the `WithHttpDataForRaygun` method to capture the HTTP request and response data._

### Configuration

All parameters to `WithHttpDataForRaygun` are optional.

```csharp
Log.Logger = new LoggerConfiguration()
  .WriteTo.Raygun("RaygunAPIKey")
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

### ThrowOnError
`type: bool`

`default: false`

This is false by default, which means that any exception that occur within Raygun4Net itself will be silently caught. Setting this to true will allow any exceptions occurring in Raygun4Net to be thrown, which can help debug issues with Raygun4Net if crash reports aren't showing up in Raygun.

### IgnoreSensitiveFieldNames
`type: string[]`

`default: null`

Crash reports sent to Raygun from this Serilog sink will include HTTP context details if present. (Currently only supported in .NET Framework applications). `IgnoreSensitiveFieldNames` lets you specify a list of HTTP query parameters, form fields, headers, cookies and server variables that you do not want to be sent to Raygun. Additionally, entries in this setting will be attempted to be stripped out of the raw request payload (more options for controlling this are explained in the `IsRawDataIgnored` section below).

Setting `IgnoreSensitiveFieldNames` to a list that only contains "*" will cause none of these things to be sent to Raygun. Placing * before, after or at both ends of an entry will perform an ends-with, starts-with or contains operation respectively.

Individual options are also available which function in the same way as IgnoreSensitiveFieldNames: `IgnoreQueryParameterNames`, `IgnoreFormFieldNames`, `IgnoreHeaderNames`, `IgnoreCookieNames` and `IgnoreServerVariableNames`.

The `IgnoreFormFieldNames` entries will also strip out specified values from the raw request payload if it is multipart/form-data.

### IsRawDataIgnored
`type: bool`

`default: false`

By default, Raygun crash reports will capture the raw request payload of the current HTTP context if present. (Currently only supported in .NET Framework applications). If you would not like to include raw request payloads on crash reports sent to Raygun, then you can set `IsRawDataIgnored` to true.

If you do want to include the raw request payload, but want to filter out sensitive fields, then you can use the `IgnoreSensitiveFieldNames` options described above. You'll also need to specify how the fields should be stripped from the raw request payload. Set `UseXmlRawDataFilter` to true for XML payloads or/and set `UseKeyValuePairRawDataFilter` to true for payloads of the format "key1=value1&key2=value2".

Setting `IsRawDataIgnoredWhenFilteringFailed` to true will cause the entire raw request payload to be ignored in cases where specified sensitive values fail to be stripped out.

### CrashReportingOfflineStorageEnabled
`type: bool`

`default: true`

Only available in .NET Framework applications. This is true by default which will cause crash reports to be saved to isolated storage (if possible) in cases where they fail to be sent to Raygun. This option lets you disable this functionality by setting it to false. When enabled, a maximum of 64 crash reports can be saved. This limit can be set lower than 64 via the `MaxCrashReportsStoredOffline` option.
