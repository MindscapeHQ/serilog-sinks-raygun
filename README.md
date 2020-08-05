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

This is null by default, so you need to configure the userInfoProperty name if you want to log more user information in this way. This will cause the RaygunIdentifierMessage to be included in the "User" section of the Raygun payload, allowing the information to be picked up by the "Users" section of the Raygun service. This will not happen if the RaygunIdentifierMessage is destructured into the log message. Sending user information in this way will overwrite the use of the userNameProperty.

The user identifier passed into the RaygunIdentifierMessage constructor could be the users name, email address, database id or whatever works best for you to identify unique users.

```csharp
var userInfo = new RaygunIdentifierMessage("12345")
{
    FirstName = "John",
    FullName = "John Doe",
    Email = "johndoe@email.address"
};

Log.ForContext("CustomUserInfoProperty", userInfo).Error(new Exception("random error"), "other information");
```