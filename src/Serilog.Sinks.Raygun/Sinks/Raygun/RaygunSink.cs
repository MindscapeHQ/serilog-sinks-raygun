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
    /// Writes log events to the Raygun service.
    /// </summary>
    public class RaygunSink : ILogEventSink
    {
        private const string RenderedLogMessageProperty = "RenderedLogMessage";
        private const string LogMessageTemplateProperty = "LogMessageTemplate";
        private const string OccurredProperty = "RaygunSink_OccurredOn";

        private readonly IFormatProvider _formatProvider;
        private readonly string _userNameProperty;
        private readonly string _applicationVersionProperty;
        private readonly IEnumerable<string> _tags;
        private readonly string _groupKeyProperty;
        private readonly string _tagsProperty;
        private readonly string _userInfoProperty;
        private readonly RaygunClient _client;
        private readonly Action<OnBeforeSendParameters> _onBeforeSend;

        /// <summary>
        /// Construct a sink that saves errors to the Raygun service. Properties and the log message are being attached as UserCustomData and the level is included as a Tag.
        /// </summary>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="applicationKey">The application key as found on an application in your Raygun account.</param>
        /// <param name="wrapperExceptions">If you have common outer exceptions that wrap a valuable inner exception which you'd prefer to group by, you can specify these by providing a list.</param>
        /// <param name="userNameProperty">Specifies the property name to read the username from. By default it is UserName. Set to null if you do not want to use this feature.</param>
        /// <param name="applicationVersionProperty">Specifies the property to use to retrieve the application version from. You can use an enricher to add the application version to all the log events. When you specify null, Raygun will use the assembly version.</param>
        /// <param name="tags">Specifies the tags to include with every log message. The log level will always be included as a tag.</param>
        /// <param name="ignoredFormFieldNames">Specifies the form field names which to ignore when including request form data.</param>
        /// <param name="groupKeyProperty">The property containing the custom group key for the Raygun message.</param>
        /// <param name="tagsProperty">The property where additional tags are stored when emitting log events.</param>
        /// <param name="userInfoProperty">The property where a RaygunIdentifierMessage with more user information can optionally be provided.</param>
        /// <param name="onBeforeSend">The action to be executed right before a logging message is sent to Raygun</param>
        public RaygunSink(IFormatProvider formatProvider,
            string applicationKey,
            IEnumerable<Type> wrapperExceptions = null,
            string userNameProperty = "UserName",
            string applicationVersionProperty = "ApplicationVersion",
            IEnumerable<string> tags = null,
            IEnumerable<string> ignoredFormFieldNames = null,
            string groupKeyProperty = "GroupKey",
            string tagsProperty = "Tags",
            string userInfoProperty = null,
            Action<OnBeforeSendParameters> onBeforeSend = null)
        {
            _formatProvider = formatProvider;
            _userNameProperty = userNameProperty;
            _applicationVersionProperty = applicationVersionProperty;
            _tags = tags ?? new string[0];
            _groupKeyProperty = groupKeyProperty;
            _tagsProperty = tagsProperty;
            _userInfoProperty = userInfoProperty;
            _onBeforeSend = onBeforeSend;
            

#if NETSTANDARD2_0
            _client = new RaygunClient(applicationKey);
#else
            _client = string.IsNullOrWhiteSpace(applicationKey) ? new RaygunClient() : new RaygunClient(applicationKey);
#endif

            // Raygun4Net adds these two wrapper exceptions by default, but as there is no way to remove them through this Serilog sink, we replace them entirely with the configured wrapper exceptions.
            _client.RemoveWrapperExceptions(typeof(TargetInvocationException), Type.GetType("System.Web.HttpUnhandledException, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));

            if (wrapperExceptions != null)
                _client.AddWrapperExceptions(wrapperExceptions.ToArray());

            if (ignoredFormFieldNames != null)
                _client.IgnoreFormFieldNames(ignoredFormFieldNames.ToArray());

            _client.CustomGroupingKey += OnCustomGroupingKey;
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            // Include the log level as a tag.
            var tags = _tags.Concat(new[] { logEvent.Level.ToString() }).ToList();

            var properties = logEvent.Properties.ToDictionary(kv => kv.Key, kv => kv.Value);

            // Add the message and template to the properties
            properties[RenderedLogMessageProperty] = new ScalarValue(logEvent.RenderMessage(_formatProvider));
            properties[LogMessageTemplateProperty] = new ScalarValue(logEvent.MessageTemplate.Text);
            properties[OccurredProperty] = new ScalarValue(logEvent.Timestamp.UtcDateTime);

            // Add additional custom tags
            if (properties.TryGetValue(_tagsProperty, out var eventTags) && eventTags is SequenceValue tagsSequence)
            {
                tags.AddRange(tagsSequence.Elements.Select(t => t.ToString("l", null)));

                properties.Remove(_tagsProperty);
            }

            // Decide what exception object to send
            var exception = logEvent.Exception ?? new NullException(GetCurrentExecutionStackTrace());

            // Submit
            if (logEvent.Level == LogEventLevel.Fatal)
            {
                _client.Send(exception, tags, properties);
            }
            else
            {
                _client.SendInBackground(exception, tags, properties);
            }
        }

        private void OnCustomGroupingKey(object sender, RaygunCustomGroupingKeyEventArgs e)
        {
            if (e?.Message?.Details != null)
            {
                var details = e.Message.Details;

                details.Client = new RaygunClientMessage
                {
                    Name = "RaygunSerilogSink",
                    Version = new AssemblyName(this.GetType().Assembly.FullName).Version.ToString(),
                    ClientUrl = "https://github.com/serilog/serilog-sinks-raygun"
                };

                if (details.UserCustomData is Dictionary<string, LogEventPropertyValue> properties)
                {
                    // If an Exception has not been provided, then use the log message/template to fill in the details and attach the current execution stack
                    if (e.Exception is NullException nullException)
                    {
                        details.Error = new RaygunErrorMessage
                        {
                            ClassName = properties[LogMessageTemplateProperty].AsString(),
                            Message = properties[RenderedLogMessageProperty].AsString(),
                            StackTrace = RaygunErrorMessageBuilder.BuildStackTrace(nullException.CodeExecutionStackTrace)
                        };
                    }

                    if (properties.TryGetValue(OccurredProperty, out var occurredOnPropertyValue) &&
                        occurredOnPropertyValue is ScalarValue occurredOnScalar &&
                        occurredOnScalar.Value is DateTime occurredOn)
                    {
                        e.Message.OccurredOn = occurredOn;

                        properties.Remove(OccurredProperty);
                    }

                    // Add user information if provided
                    if (!string.IsNullOrWhiteSpace(_userInfoProperty) &&
                        properties.TryGetValue(_userInfoProperty, out var userInfoPropertyValue) &&
                        userInfoPropertyValue != null)
                    {
                        switch (userInfoPropertyValue)
                        {
                            case StructureValue userInfoStructure:
                                details.User = BuildUserInformationFromStructureValue(userInfoStructure);
                                break;
                            case ScalarValue userInfoScalar when userInfoScalar.Value is string userInfo:
                                details.User = ParseUserInformation(userInfo);
                                break;
                        }

                        if (details.User != null)
                        {
                            details.UserCustomData.Remove(_userInfoProperty);
                        }
                    }

                    // If user information is not set, then use the user-name if provided
                    if (details.User == null &&
                        !string.IsNullOrWhiteSpace(_userNameProperty) &&
                        properties.ContainsKey(_userNameProperty) &&
                        properties[_userNameProperty] != null)
                    {
                        details.User = new RaygunIdentifierMessage(properties[_userNameProperty].AsString());

                        properties.Remove(_userNameProperty);
                    }

                    // Add version if provided
                    if (!string.IsNullOrWhiteSpace(_applicationVersionProperty) &&
                        properties.ContainsKey(_applicationVersionProperty) &&
                        properties[_applicationVersionProperty] != null)
                    {
                        details.Version = properties[_applicationVersionProperty].AsString();

                        properties.Remove(_applicationVersionProperty);
                    }

                    // Add the custom group key if provided
                    if (properties.TryGetValue(_groupKeyProperty, out var customKey))
                    {
                        details.GroupingKey = customKey.AsString();

                        properties.Remove(_groupKeyProperty);
                    }

#if NETSTANDARD2_0
                    // Add Http request/response messages if present and not already set
                    if (details.Request == null &&
                        properties.TryGetValue(RaygunClientHttpEnricher.RaygunRequestMessagePropertyName, out var requestMessageProperty) &&
                        requestMessageProperty is StructureValue requestMessageValue)
                    {
                        details.Request = BuildRequestMessageFromStructureValue(requestMessageValue);
                        properties.Remove(RaygunClientHttpEnricher.RaygunRequestMessagePropertyName);
                    }

                    if (details.Response == null &&
                        properties.TryGetValue(RaygunClientHttpEnricher.RaygunResponseMessagePropertyName, out var responseMessageProperty) &&
                        responseMessageProperty is StructureValue responseMessageValue)
                    {
                        details.Response = BuildResponseMessageFromStructureValue(responseMessageValue);
                        properties.Remove(RaygunClientHttpEnricher.RaygunResponseMessagePropertyName);
                    }
#endif

                    // Simplify the remaining properties to be used as user-custom-data
                    details.UserCustomData = properties
                        .Select(pv => new { Name = pv.Key, Value = RaygunPropertyFormatter.Simplify(pv.Value) })
                        .ToDictionary(a => a.Name, b => b.Value);
                }
            }
            
            // Call onBeforeSend
            if (_onBeforeSend != null)
            {
                var onBeforeSendParameters = new OnBeforeSendParameters(e?.Exception, e?.Message);
                _onBeforeSend(onBeforeSendParameters);
            }
        }

        private static StackTrace GetCurrentExecutionStackTrace()
        {
            StackTrace stackTrace = new StackTrace();

            for (int frameIndex = 0; frameIndex < stackTrace.FrameCount; frameIndex++)
            {
                MethodBase method = stackTrace.GetFrame(frameIndex).GetMethod();
                string className = method?.ReflectedType?.FullName ?? "";

                if (!className.StartsWith("Serilog."))
                {
                    return new StackTrace(frameIndex);
                }
            }

            return stackTrace;
        }

        private static RaygunIdentifierMessage BuildUserInformationFromStructureValue(StructureValue userStructure)
        {
            RaygunIdentifierMessage userIdentifier = new RaygunIdentifierMessage(null);

            foreach (var property in userStructure.Properties)
            {
                switch (property.Name)
                {
                    case nameof(RaygunIdentifierMessage.Identifier):
                        userIdentifier.Identifier = property.AsString();
                        break;
                    case nameof(RaygunIdentifierMessage.IsAnonymous):
                        userIdentifier.IsAnonymous = "True".Equals(property.Value.ToString());
                        break;
                    case nameof(RaygunIdentifierMessage.Email):
                        userIdentifier.Email = property.AsString();
                        break;
                    case nameof(RaygunIdentifierMessage.FullName):
                        userIdentifier.FullName = property.AsString();
                        break;
                    case nameof(RaygunIdentifierMessage.FirstName):
                        userIdentifier.FirstName = property.AsString();
                        break;
                    case nameof(RaygunIdentifierMessage.UUID):
                        userIdentifier.UUID = property.AsString();
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

        private static RaygunResponseMessage BuildResponseMessageFromStructureValue(StructureValue responseMessageStructure)
        {
            var responseMessage = new RaygunResponseMessage();

            foreach (var property in responseMessageStructure.Properties)
            {
                switch (property.Name)
                {
                    case nameof(RaygunResponseMessage.Content):
                        responseMessage.Content = property.AsString();
                        break;
                    case nameof(RaygunResponseMessage.StatusCode):
                        responseMessage.StatusCode = property.AsInteger();
                        break;
                    case nameof(RaygunResponseMessage.StatusDescription):
                        responseMessage.StatusDescription = property.AsString();
                        break;
                }
            }

            return responseMessage;
        }

        private static RaygunRequestMessage BuildRequestMessageFromStructureValue(StructureValue requestMessageStructure)
        {
            var requestMessage = new RaygunRequestMessage();

            foreach (var property in requestMessageStructure.Properties)
            {
                switch (property.Name)
                {
                    case nameof(RaygunRequestMessage.Url):
                        requestMessage.Url = property.AsString();
                        break;
                    case nameof(RaygunRequestMessage.HostName):
                        requestMessage.HostName = property.AsString();
                        break;
                    case nameof(RaygunRequestMessage.HttpMethod):
                        requestMessage.HttpMethod = property.AsString();
                        break;
                    case nameof(RaygunRequestMessage.IPAddress):
                        requestMessage.IPAddress = property.AsString();
                        break;
                    case nameof(RaygunRequestMessage.RawData):
                        requestMessage.RawData = property.AsString();
                        break;
                    case nameof(RaygunRequestMessage.Headers):
                        requestMessage.Headers = property.AsDictionary();
                        break;
                    case nameof(RaygunRequestMessage.QueryString):
                        requestMessage.QueryString = property.AsDictionary();
                        break;
                    case nameof(RaygunRequestMessage.Form):
                        requestMessage.Form = property.AsDictionary();
                        break;
                    case nameof(RaygunRequestMessage.Data):
                        requestMessage.Data = property.AsDictionary();
                        break;
                }
            }

            return requestMessage;
        }
    }
}
