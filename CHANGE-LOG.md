# Full Change Log for Serilog.Sinks.Raygun package

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
 