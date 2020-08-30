// Copyright 2014 Serilog Contributors
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Mindscape.Raygun4Net;
#if NETSTANDARD2_0
using Mindscape.Raygun4Net.AspNetCore;
#else
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Messages;
#endif
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Raygun
{
    /// <summary>
    /// Writes log events to the Raygun.com service.
    /// </summary>
    public class RaygunSink : ILogEventSink
    {
        readonly IFormatProvider _formatProvider;
        readonly string _userNameProperty;
        readonly string _applicationVersionProperty;
        readonly IEnumerable<string> _tags;
        readonly IEnumerable<string> _ignoredFormFieldNames;
        readonly string _groupKeyProperty;
        readonly string _tagsProperty;
        readonly string _userInfoProperty;
        readonly RaygunClient _client;

        /// <summary>
        /// Construct a sink that saves errors to the Raygun.io service. Properties are being send as userdata and the level is included as tag. The message is included inside the userdata.
        /// </summary>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="applicationKey">The application key as found on the Raygun website.</param>
        /// <param name="wrapperExceptions">If you have common outer exceptions that wrap a valuable inner exception which you'd prefer to group by, you can specify these by providing a list.</param>
        /// <param name="userNameProperty">Specifies the property name to read the username from. By default it is UserName. Set to null if you do not want to use this feature.</param>
        /// <param name="applicationVersionProperty">Specifies the property to use to retrieve the application version from. You can use an enricher to add the application version to all the log events. When you specify null, Raygun will use the assembly version.</param>
        /// <param name="tags">Specifies the tags to include with every log message. The log level will always be included as a tag.</param>
        /// <param name="ignoredFormFieldNames">Specifies the form field names which to ignore when including request form data.</param>
        /// <param name="groupKeyProperty">The property containing the custom group key for the Raygun message.</param>
        /// <param name="tagsProperty">The property where additional tags are stored when emitting log events.</param>
        /// <param name="userInfoProperty">The property where a RaygunIdentifierMessage with more user information can optionally be provided.</param>
        public RaygunSink(IFormatProvider formatProvider,
            string applicationKey,
            IEnumerable<Type> wrapperExceptions = null,
            string userNameProperty = "UserName",
            string applicationVersionProperty = "ApplicationVersion",
            IEnumerable<string> tags = null,
            IEnumerable<string> ignoredFormFieldNames = null,
            string groupKeyProperty = "GroupKey",
            string tagsProperty = "Tags",
            string userInfoProperty = null)
        {
            if (string.IsNullOrEmpty(applicationKey))
                throw new ArgumentNullException("applicationKey");

            _formatProvider = formatProvider;
            _userNameProperty = userNameProperty;
            _applicationVersionProperty = applicationVersionProperty;
            _tags = tags ?? new string[0];
            _ignoredFormFieldNames = ignoredFormFieldNames ?? Enumerable.Empty<string>();
            _groupKeyProperty = groupKeyProperty;
            _tagsProperty = tagsProperty;
            _userInfoProperty = userInfoProperty;

            _client = new RaygunClient(applicationKey);
            if (wrapperExceptions != null)
                _client.AddWrapperExceptions(wrapperExceptions.ToArray());
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            //Include the log level as a tag.
            var tags = _tags.Concat(new[] { logEvent.Level.ToString() }).ToList();

            var properties = logEvent.Properties
                         .Select(pv => new { Name = pv.Key, Value = RaygunPropertyFormatter.Simplify(pv.Value) })
                         .ToDictionary(a => a.Name, b => b.Value);

            // Add the message
            properties.Add("RenderedLogMessage", logEvent.RenderMessage(_formatProvider));
            properties.Add("LogMessageTemplate", logEvent.MessageTemplate.Text);

            // Create new message
            var raygunMessage = new RaygunMessage
            {
                OccurredOn = logEvent.Timestamp.UtcDateTime
            };

            // Add exception when available, else use the message template so events can be grouped
            raygunMessage.Details.Error = logEvent.Exception != null
                ? RaygunErrorMessageBuilder.Build(logEvent.Exception)
                : new RaygunErrorMessage()
                {
                    ClassName = logEvent.MessageTemplate.Text,
                    Message = logEvent.RenderMessage(_formatProvider),
                    Data = logEvent.Properties.ToDictionary(k => k.Key, v => v.Value.ToString()),
                    StackTrace = BuildStackTrace(new StackTrace())
                };

            // Add user when requested
            if (!string.IsNullOrWhiteSpace(_userInfoProperty) &&
                logEvent.Properties.TryGetValue(_userInfoProperty, out var userInfoPropertyValue) &&
                userInfoPropertyValue != null)
            {
                switch (userInfoPropertyValue)
                {
                    case StructureValue userInfoStructure:
                        raygunMessage.Details.User = BuildUserInformationFromStructureValue(userInfoStructure);
                        break;
                    case ScalarValue userInfoScalar when userInfoScalar.Value is string userInfo:
                        raygunMessage.Details.User = ParseUserInformation(userInfo);
                        break;
                }

                if (raygunMessage.Details.User != null)
                {
                    properties.Remove(_userInfoProperty);
                }
            }

            if (raygunMessage.Details.User == null &&
                !string.IsNullOrWhiteSpace(_userNameProperty) &&
                logEvent.Properties.ContainsKey(_userNameProperty) &&
                logEvent.Properties[_userNameProperty] != null)
            {
                raygunMessage.Details.User = new RaygunIdentifierMessage(logEvent.Properties[_userNameProperty].ToString());

                properties.Remove(_userNameProperty);
            }

            // Add version when requested
            if (!String.IsNullOrWhiteSpace(_applicationVersionProperty) &&
                logEvent.Properties.ContainsKey(_applicationVersionProperty) &&
                logEvent.Properties[_applicationVersionProperty] != null)
            {
                raygunMessage.Details.Version = logEvent.Properties[_applicationVersionProperty].ToString("l", null);

                properties.Remove(_applicationVersionProperty);
            }

            // Build up the rest of the message
#if NETSTANDARD2_0
            raygunMessage.Details.Environment = RaygunEnvironmentMessageBuilder.Build(new RaygunSettings());
#else
            raygunMessage.Details.Environment = RaygunEnvironmentMessageBuilder.Build();
#endif
            raygunMessage.Details.Tags = tags;
            raygunMessage.Details.MachineName = Environment.MachineName;

            raygunMessage.Details.Client = new RaygunClientMessage()
            {
                Name = "RaygunSerilogSink",
                Version = new AssemblyName(this.GetType().Assembly.FullName).Version.ToString(),
                ClientUrl = "https://github.com/serilog/serilog-sinks-raygun"
            };

            // Add the custom group key when provided
            if (properties.TryGetValue(_groupKeyProperty, out var customKey))
            {
                raygunMessage.Details.GroupingKey = customKey.ToString();

                properties.Remove(_groupKeyProperty);
            }

            // Add additional custom tags
            if (properties.TryGetValue(_tagsProperty, out var eventTags) && eventTags is object[])
            {
                foreach (var tag in (object[])eventTags)
                    raygunMessage.Details.Tags.Add(tag.ToString());

                properties.Remove(_tagsProperty);
            }

            raygunMessage.Details.UserCustomData = properties;

            // Submit
            if (logEvent.Level == LogEventLevel.Fatal)
            {
                _client.Send(raygunMessage);
            }
            else
            {
                _client.SendInBackground(raygunMessage);
            }
        }

        private static RaygunIdentifierMessage BuildUserInformationFromStructureValue(StructureValue userStructure)
        {
            RaygunIdentifierMessage userIdentifier = new RaygunIdentifierMessage(null);

            foreach (var property in userStructure.Properties)
            {
                ScalarValue scalar = property.Value as ScalarValue;
                switch (property.Name)
                {
                    case nameof(RaygunIdentifierMessage.Identifier):
                        userIdentifier.Identifier = scalar?.Value != null ? property.Value.ToString("l", null) : null;
                        break;
                    case nameof(RaygunIdentifierMessage.IsAnonymous):
                        userIdentifier.IsAnonymous = "True".Equals(property.Value.ToString());
                        break;
                    case nameof(RaygunIdentifierMessage.Email):
                        userIdentifier.Email = scalar?.Value != null ? property.Value.ToString("l", null) : null;
                        break;
                    case nameof(RaygunIdentifierMessage.FullName):
                        userIdentifier.FullName = scalar?.Value != null ? property.Value.ToString("l", null) : null;
                        break;
                    case nameof(RaygunIdentifierMessage.FirstName):
                        userIdentifier.FirstName = scalar?.Value != null ? property.Value.ToString("l", null) : null;
                        break;
                    case nameof(RaygunIdentifierMessage.UUID):
                        userIdentifier.UUID = scalar?.Value != null ? property.Value.ToString("l", null) : null;
                        break;
                }
            }

            return userIdentifier;
        }

        private static RaygunIdentifierMessage ParseUserInformation(string userInfo)
        {
            RaygunIdentifierMessage userIdentifier = null;

            // This is a parse of the ToString implementation of RaygunIdentifierMessage which uses the format:
            // [RaygunIdentifierMessage: Identifier=X, IsAnonymous=X, Email=X, FullName=X, FirstName=X, UUID=X]
            string[] properties = userInfo.Split(new[] { ',', ']' }, StringSplitOptions.RemoveEmptyEntries);
            if (properties.Length == 6)
            {
                string[] identifierSplit = properties[0].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (identifierSplit.Length == 2)
                {
                    userIdentifier = new RaygunIdentifierMessage(identifierSplit[1]);

                    string[] isAnonymousSplit = properties[1].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (isAnonymousSplit.Length == 2)
                    {
                        userIdentifier.IsAnonymous = "True".Equals(isAnonymousSplit[1]);
                    }

                    string[] emailSplit = properties[2].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (emailSplit.Length == 2)
                    {
                        userIdentifier.Email = emailSplit[1];
                    }

                    string[] fullNameSplit = properties[3].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fullNameSplit.Length == 2)
                    {
                        userIdentifier.FullName = fullNameSplit[1];
                    }

                    string[] firstNameSplit = properties[4].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (firstNameSplit.Length == 2)
                    {
                        userIdentifier.FirstName = firstNameSplit[1];
                    }

                    string[] uuidSplit = properties[5].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (uuidSplit.Length == 2)
                    {
                        userIdentifier.UUID = uuidSplit[1];
                    }
                }
            }

            return userIdentifier;
        }

        private static RaygunErrorStackTraceLineMessage[] BuildStackTrace(StackTrace stackTrace)
        {
            var lines = new List<RaygunErrorStackTraceLineMessage>();

            var frames = stackTrace.GetFrames();

            if (frames == null || frames.Length == 0)
            {
                var line = new RaygunErrorStackTraceLineMessage { FileName = "none", LineNumber = 0 };
                lines.Add(line);
                return lines.ToArray();
            }

            foreach (StackFrame frame in frames)
            {
                MethodBase method = frame.GetMethod();

                if (method != null)
                {
                    int lineNumber = frame.GetFileLineNumber();

                    if (lineNumber == 0)
                    {
                        lineNumber = frame.GetILOffset();
                    }

                    var methodName = GenerateMethodName(method);

                    string file = frame.GetFileName();

                    string className = method.ReflectedType != null ? method.ReflectedType.FullName : "(unknown)";

                    var line = new RaygunErrorStackTraceLineMessage
                    {
                        FileName = file,
                        LineNumber = lineNumber,
                        MethodName = methodName,
                        ClassName = className
                    };

                    lines.Add(line);
                }
            }

            return lines.ToArray();
        }

        private static string GenerateMethodName(MethodBase method)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(method.Name);

            bool first = true;
            if (method is MethodInfo && method.IsGenericMethod)
            {
                Type[] genericArguments = method.GetGenericArguments();
                stringBuilder.Append("[");
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (!first)
                    {
                        stringBuilder.Append(",");
                    }
                    else
                    {
                        first = false;
                    }

                    stringBuilder.Append(genericArguments[i].Name);
                }

                stringBuilder.Append("]");
            }

            stringBuilder.Append("(");
            ParameterInfo[] parameters = method.GetParameters();
            first = true;
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (!first)
                {
                    stringBuilder.Append(", ");
                }
                else
                {
                    first = false;
                }

                string type = "<UnknownType>";
                if (parameters[i].ParameterType != null)
                {
                    type = parameters[i].ParameterType.Name;
                }

                stringBuilder.Append(type + " " + parameters[i].Name);
            }

            stringBuilder.Append(")");

            return stringBuilder.ToString();
        }
    }
}
