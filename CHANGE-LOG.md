# Full Change Log for Serilog.Sinks.Raygun package


### v8.0.0
- Updated Raygun4Net dependency
- Added `ApplicationBuilderExtensions` to allow `AddRaygun(...)` for use in Console/Services
- Implemented new `RaygunSinkV2` which is cut down but supports using whatever RaygunClient is registered via DI
  - This treats RaygunClient as a singleton and uses it to send messages
  - Adds support for using Raygun4Net.AspNetCore package to register RaygunClient in DI
  - Adds support for `IRaygunUserProvider` to be registered in DI
  - See: https://github.com/MindscapeHQ/serilog-sinks-raygun/pull/71
- Notes: RaygunSink is left unchanged, this should not be a breaking change for anyone using the package as it is, 
         but the new `RaygunSinkV2` is available for those who wish to use it. The new sink is more flexible and allows 
         for custom RaygunClient instances to be used.

### v7.5.0
- Fixed issue in RaygunSink where it was not setting Request/Response messages if supplied by the code Enricher
- Bumped Raygun.NetCore dependency to 8.2.0 to fix issue where custom RaygunClient with HttpClient could not be used

### v7.4.0
- Drop Serilog dependency from 3.0.0 to 2.12.0
- Include new [Enricher Readme](README-ENRICHER.md) in repo for those who still wish to use the ASP.NET Core Enricher
  - An example of this usage can be found in Serilog.Sinks.Raygun.SampleWebApp project

### v7.3.0
- Drop ASP.NET Core Enricher (use `Serilog.AspNetCore` package instead)
- Removes dependency on `Mindscape.Raygun4Net.AspNetCore` package
  - This fixes an issue where Serilog couldn't be used in MAUI apps due to the dependency on `Microsoft.AspNetCore.App` which is not available in MAUI apps
- Add ability to specify `RaygunClient` instance to use by implementing `IRaygunClientProvider`
  - This allows for custom configuration of the `RaygunClient` instance
  - This also allows for using the `RaygunClient` instance from the `Mindscape.Raygun4Net` package

### v7.2.0
- Add conditional build for MAUI targets to drop ASP .NET Core dependency
 