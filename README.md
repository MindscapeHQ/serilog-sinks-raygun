# serilog-sinks-raygun

[![Build status](https://ci.appveyor.com/api/projects/status/bol0v48ujapxobym/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-raygun/branch/master)

A Serilog sink that writes events to Raygun

## Usage

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
### Required
#### applicationKey
`string`

### Optional
#### wrapperExceptions
`type: IEnumerable<Exception>`

`default: null`

#### userNameProperty
`type: string`

`default: UserName`

```csharp
Log.ForContext("CustomUserNameProperty", "John Doe").Error(new Exception("random error"), "other information");
```

#### applicationVersionProperty
`type: string`

`default: ApplicationVersion`

```csharp
Log.ForContext("CustomAppVersionProperty", "1.2.11").Error(new Exception("random error"), "other information");
```

#### restrictedToMinimumLevel
`type: LogEventLevel`

`default: LogEventLevel.Error`

#### formatProvider
`type: IFormatProvider`

`default: null`

#### tags
`type: IEnumerable<string>`

`default: null`

#### ignoredFormFieldNames
`type: IEnumerable<string>`

`default: null`

#### groupKeyProperty
`type: string`

`default: GroupKey`

```csharp
Log.ForContext("CustomGroupKeyProperty", "TransactionId-12345").Error(new Exception("random error"), "other information");
```

#### tagsProperty
`type: string`

`default: Tags`

```csharp
Log.ForContext("CustomTagsProperty", new[] {"tag1", "tag2"}).Error(new Exception("random error"), "other information");
Log.Error(new Exception("random error"), "other information {@CustomTagsProperty}", new[] {"tag3", "tag4"});
```

#### userInfoProperty
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

## Raygun4Net features

This sink wraps the [Raygun4Net](https://github.com/MindscapeHQ/raygun4net) provider to build a crash report from an Exception and send it to Raygun. This makes the following Raygun4Net features available to you. To use these features, you need to add RaygunSettings to your configuration as explained below which is separate to the Serilog configuration.

*.NET Core*

Add a RaygunSettings block to your appsettings.config file where you can populate the settings that you want to use.

```json
"RaygunSettings": {
  "Setting": "Value"
}
```

*.NET Framework*

Add the following section within the configSections element of your app.config or web.config file.

```xml
<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>
```

Then add a RaygunSettings element containing the desired settings somewhere within the configuration element of the app.config or web.config file.

```xml
<RaygunSettings setting="value"/>
```

### ThrowOnError

This is false by default, which means that any exception that occur within Raygun4Net itself will be silently caught. Setting this to true will allow any exceptions occurring in Raygun4Net to be thrown, which can help debug issues in Raygun4Net if crash reports aren't showing up in Raygun.