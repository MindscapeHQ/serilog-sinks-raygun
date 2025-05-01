#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;
using Mindscape.Raygun4Net;

namespace Serilog.Sinks.Raygun;

#if NET
/// <summary>
/// RaygunSink for Serilog which handles logging to Raygun.
/// </summary>
public class RaygunClientSink : ILogEventSink
{
    private const string RenderedLogMessageProperty = "RenderedLogMessage";
    private const string LogMessageTemplateProperty = "LogMessageTemplate";
    private const string OccurredProperty = "RaygunSink_OccurredOn";
    private const string RaygunRequestMessagePropertyName = "RaygunSink_RequestMessage";
    private const string RaygunResponseMessagePropertyName = "RaygunSink_ResponseMessage";
    private const string RaygunUserInfoPropertyName = "RaygunSink_UserInfo";

    private readonly IFormatProvider? _formatProvider;
    private readonly IEnumerable<string> _tags;
    private readonly string _tagsProperty;
    private readonly Func<LogEvent, RaygunIdentifierMessage?>? _userInfoCallback;
    private readonly RaygunClientBase _raygunClient;

    /// <summary>
    /// Construct a sink that saves errors to the Raygun service. Properties and the log message are being attached as UserCustomData and the level is included as a Tag.
    /// </summary>
    /// <param name="raygunClient">Instance of RaygunClient which should be passed in by resolving from a DI container or a static instance.</param>
    /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
    /// <param name="tags">Specifies the tags to include with every log message. The log level will always be included as a tag.</param>
    /// <param name="tagsProperty">The property where additional tags are stored when emitting log events.</param>
    /// <param name="userInfoCallback">A function to extract user information from the log event.</param>
    public RaygunClientSink(RaygunClientBase raygunClient,
        IFormatProvider? formatProvider = null,
        IEnumerable<string>? tags = null,
        string tagsProperty = "Tags",
        Func<LogEvent, RaygunIdentifierMessage?>? userInfoCallback = null
    )
    {
        _raygunClient = raygunClient;
        _formatProvider = formatProvider;
        _tags = tags ?? Array.Empty<string>();
        _tagsProperty = tagsProperty;
        _userInfoCallback = userInfoCallback;

        _raygunClient.CustomGroupingKey += OnCustomGroupingKey;

        // Raygun4Net adds these two wrapper exceptions by default, but as there is no way to remove them through this Serilog sink, we replace them entirely with the configured wrapper exceptions.
        _raygunClient.RemoveWrapperExceptions(typeof(TargetInvocationException),
            Type.GetType("System.Web.HttpUnhandledException, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
    }

    /// <summary>
    /// Emit the provided log event to the sink.
    /// </summary>
    /// <param name="logEvent">The log event to write.</param>
    public void Emit(LogEvent logEvent)
    {
        // Include the log level as a tag.
        var tags = _tags.Concat([logEvent.Level.ToString()]).ToList();
        var properties = logEvent.Properties.ToDictionary(kv => kv.Key, kv => kv.Value);

        // Add the message and template to the properties
        properties[RenderedLogMessageProperty] = new ScalarValue(logEvent.RenderMessage(_formatProvider));
        properties[LogMessageTemplateProperty] = new ScalarValue(logEvent.MessageTemplate.Text);
        properties[OccurredProperty] = new ScalarValue(logEvent.Timestamp.UtcDateTime);
        
        // Add user info if callback is provided
        if (_userInfoCallback != null)
        {
            var userInfo = _userInfoCallback(logEvent);
            if (userInfo != null)
            {
                properties[RaygunUserInfoPropertyName] = new ScalarValue(userInfo); // Store as simple scalar
            }
        }

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
            // Fatal is prob going to crash the app so let's try to send the log to raygun synchronously
            _raygunClient.SendAsync(exception, tags, properties).GetAwaiter().GetResult();
        }
        else
        {
            // Discard the task, we don't care about the result
            _ = _raygunClient.SendInBackground(exception, tags, properties);
        }
    }

    private void OnCustomGroupingKey(object? sender, RaygunCustomGroupingKeyEventArgs e)
    {
        // Delegate processing to the static method
        ProcessRaygunMessageDetails(e.Message, e.Exception);
    }

    // Extracted core logic for testability
    public static void ProcessRaygunMessageDetails(RaygunMessage message, Exception exception)
    {
        if (message?.Details != null)
        {
            var details = message.Details;
    
            details.Client = new RaygunClientMessage
            {
                Name = "RaygunSerilogSink",
                Version = typeof(RaygunClientSink).Assembly.GetName().Version?.ToString() ?? string.Empty,
                ClientUrl = "https://github.com/serilog/serilog-sinks-raygun"
            };
    
            // Handle both the internal Dictionary<,> and potential Hashtable/IDictionary from tests
            Dictionary<string, LogEventPropertyValue>? properties = null;
            if (details.UserCustomData is Dictionary<string, LogEventPropertyValue> dictProperties)
            {
                properties = dictProperties;
            }
            else if (details.UserCustomData is System.Collections.IDictionary dictionaryData)
            {
                // Attempt to convert IDictionary to the expected type if possible
                try
                {
                    properties = dictionaryData.Keys.Cast<object>()
                        .ToDictionary(k => k.ToString() ?? "", k => dictionaryData[k] as LogEventPropertyValue ?? new ScalarValue(dictionaryData[k]));
                }
                catch (Exception ex) // Catch potential casting/conversion errors
                {
                    // Log internally? Or ignore if conversion fails?
                    // For now, let's proceed with properties as null if conversion fails.
                    Debug.WriteLine($"Error converting UserCustomData IDictionary: {ex.Message}");
                }
            }

            if (properties != null)
            {
                // If an Exception has not been provided, then use the log message/template to fill in the details and attach the current execution stack
                if (exception is NullException nullException)
                {
                    details.Error = new RaygunErrorMessage
                    {
                        ClassName = properties[LogMessageTemplateProperty].AsString(),
                        Message = properties[RenderedLogMessageProperty].AsString(),
                        StackTrace = RaygunErrorMessageBuilder.BuildStackTrace(nullException.CodeExecutionStackTrace)
                    };
                }
    
                if (properties.TryGetValue(OccurredProperty, out var occurredOnPropertyValue) &&
                    occurredOnPropertyValue is ScalarValue { Value: DateTime occurredOn })
                {
                    message.OccurredOn = occurredOn;
    
                    properties.Remove(OccurredProperty);
                }
    
                // Add Http request/response messages if present and not already set
                if (details.Request == null &&
                    properties.TryGetValue(RaygunRequestMessagePropertyName, out var requestMessageProperty) &&
                    requestMessageProperty is StructureValue requestMessageValue)
                {
                    details.Request = BuildRequestMessageFromStructureValue(requestMessageValue);
                    properties.Remove(RaygunRequestMessagePropertyName);
                }
    
                if (details.Response == null &&
                    properties.TryGetValue(RaygunResponseMessagePropertyName, out var responseMessageProperty) &&
                    responseMessageProperty is StructureValue responseMessageValue)
                {
                    details.Response = BuildResponseMessageFromStructureValue(responseMessageValue);
                    properties.Remove(RaygunResponseMessagePropertyName);
                }

                // Add UserInfo if present and not already set
                if (details.User == null &&
                    properties.TryGetValue(RaygunUserInfoPropertyName, out var userInfoProperty) &&
                    userInfoProperty is StructureValue userInfoValue)
                {
                    details.User = BuildUserInfoFromStructureValue(userInfoValue);
                    properties.Remove(RaygunUserInfoPropertyName);
                }

                // Simplify the remaining properties to be used as user-custom-data
                details.UserCustomData = properties
                    .Select(pv => new { Name = pv.Key, Value = RaygunPropertyFormatter.Simplify(pv.Value) })
                    .ToDictionary(a => a.Name, b => b.Value);
            }
        }
    }

    private static StackTrace GetCurrentExecutionStackTrace()
    {
        var stackTrace = new StackTrace();

        for (var frameIndex = 0; frameIndex < stackTrace.FrameCount; frameIndex++)
        {
            var method = stackTrace.GetFrame(frameIndex)?.GetMethod();
            var className = method?.ReflectedType?.FullName ?? "";

            if (!className.StartsWith("Serilog."))
            {
                return new StackTrace(frameIndex);
            }
        }

        return stackTrace;
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

    // Added helper method to convert RaygunIdentifierMessage to StructureValue
    private static StructureValue BuildUserInfoStructureValue(RaygunIdentifierMessage userInfo)
    {
        var properties = new List<LogEventProperty>();

        if (userInfo.Identifier != null)
        {
            properties.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.Identifier), new ScalarValue(userInfo.Identifier)));
        }
        properties.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.IsAnonymous), new ScalarValue(userInfo.IsAnonymous)));
        if (userInfo.Email != null)
        {
            properties.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.Email), new ScalarValue(userInfo.Email)));
        }
        if (userInfo.FullName != null)
        {
            properties.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.FullName), new ScalarValue(userInfo.FullName)));
        }
        if (userInfo.FirstName != null)
        {
            properties.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.FirstName), new ScalarValue(userInfo.FirstName)));
        }
        if (userInfo.UUID != null)
        {
            properties.Add(new LogEventProperty(nameof(RaygunIdentifierMessage.UUID), new ScalarValue(userInfo.UUID)));
        }

        return new StructureValue(properties, nameof(RaygunIdentifierMessage));
    }

    // Added helper method to convert StructureValue back to RaygunIdentifierMessage
    private static RaygunIdentifierMessage BuildUserInfoFromStructureValue(StructureValue userInfoStructure)
    {
        var userInfo = new RaygunIdentifierMessage(null); // Initialize with null identifier

        foreach (var property in userInfoStructure.Properties)
        {
            switch (property.Name)
            {
                case nameof(RaygunIdentifierMessage.Identifier):
                    userInfo.Identifier = property.AsString();
                    break;
                case nameof(RaygunIdentifierMessage.IsAnonymous):
                    userInfo.IsAnonymous = property.AsBoolean();
                    break;
                case nameof(RaygunIdentifierMessage.Email):
                    userInfo.Email = property.AsString();
                    break;
                case nameof(RaygunIdentifierMessage.FullName):
                    userInfo.FullName = property.AsString();
                    break;
                case nameof(RaygunIdentifierMessage.FirstName):
                    userInfo.FirstName = property.AsString();
                    break;
                case nameof(RaygunIdentifierMessage.UUID):
                    userInfo.UUID = property.AsString();
                    break;
            }
        }

        return userInfo;
    }
}

#endif
