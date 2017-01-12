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
      "CustomTagsProperty")
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