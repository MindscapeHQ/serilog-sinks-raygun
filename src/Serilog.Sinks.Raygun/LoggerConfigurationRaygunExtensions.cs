﻿// Copyright 2014 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.Raygun;

namespace Serilog;

/// <summary>
/// Adds the WriteTo.Raygun() extension method to <see cref="LoggerConfiguration"/>.
/// </summary>
public static class LoggerConfigurationRaygunExtensions
{
    /// <summary>
    /// Adds a sink that writes log events (defaults to error and up) to the Raygun service. Properties and the log message are being attached as UserCustomData and the level is included as a Tag.
    /// Your message is part of the custom data.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration.</param>
    /// <param name="applicationKey">The application key as found on an application in your Raygun account.</param>
    /// <param name="wrapperExceptions">If you have common outer exceptions that wrap a valuable inner exception which you'd prefer to group by, you can specify these by providing a list.</param>
    /// <param name="userNameProperty">Specifies the property name to read the username from. By default it is UserName. Set to null if you do not want to use this feature.</param>
    /// <param name="applicationVersionProperty">Specifies the property to use to retrieve the application version from. You can use an enricher to add the application version to all the log events. When you specify null, Raygun will use the assembly version.</param> 
    /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink. By default set to Error as Raygun is mostly used for error reporting.</param>
    /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
    /// <param name="tags">Specifies the tags to include with every log message. The log level will always be included as a tag.</param>
    /// <param name="ignoredFormFieldNames">Specifies the form field names which to ignore when including request form data.</param>
    /// <param name="groupKeyProperty">The property containing the custom group key for the Raygun message.</param>
    /// <param name="tagsProperty">The property where additional tags are stored when emitting log events.</param>
    /// <param name="userInfoProperty">The property containing the RaygunIdentifierMessage structure used to populate user details.</param>
    /// <param name="onBeforeSend">The action to be executed right before a logging message is sent to Raygun</param>
    /// <param name="settings">Allows you to provide settings for the Raygun service. If null, defaults are used.</param>
    /// <param name="raygunClientProvider">Provides a way to customize the Raygun client. If null, a default client is used.</param>
    /// <returns>Logger configuration, allowing configuration to continue.</returns>
    /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
    public static LoggerConfiguration Raygun(
        this LoggerSinkConfiguration loggerConfiguration,
        string applicationKey,
        IEnumerable<Type> wrapperExceptions = null,
        string userNameProperty = "UserName",
        string applicationVersionProperty = "ApplicationVersion",
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Error,
        IFormatProvider formatProvider = null,
        IEnumerable<string> tags = null,
        IEnumerable<string> ignoredFormFieldNames = null,
        string groupKeyProperty = "GroupKey",
        string tagsProperty = "Tags",
        string userInfoProperty = null,
        Action<OnBeforeSendArguments> onBeforeSend = null, 
        RaygunSettings settings = null, 
        IRaygunClientProvider raygunClientProvider = null
    )
    {
        if (loggerConfiguration == null)
        {
            throw new ArgumentNullException(nameof(loggerConfiguration));
        }

        return loggerConfiguration.Sink(new RaygunSink(formatProvider,
            applicationKey,
            wrapperExceptions,
            userNameProperty,
            applicationVersionProperty,
            tags,
            ignoredFormFieldNames,
            groupKeyProperty,
            tagsProperty,
            userInfoProperty,
            onBeforeSend,
            settings,
            raygunClientProvider), restrictedToMinimumLevel);
    }
}