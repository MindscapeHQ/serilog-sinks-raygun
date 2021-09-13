﻿#if NETSTANDARD2_0
using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net;
using Mindscape.Raygun4Net.AspNetCore;
using Mindscape.Raygun4Net.AspNetCore.Builders;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Raygun
{
	public class RaygunClientHttpEnricher : ILogEventEnricher
	{
		public const string RaygunRequestMessagePropertyName = "RaygunSink_RequestMessage";
		public const string RaygunResponseMessagePropertyName = "RaygunSink_ResponseMessage";
		
		readonly IHttpContextAccessor _httpContextAccessor;
		private readonly LogEventLevel _restrictedToMinimumLevel;
		
		public RaygunClientHttpEnricher(IHttpContextAccessor httpContextAccessor, LogEventLevel restrictedToMinimumLevel)
		{
			_httpContextAccessor = httpContextAccessor;
			_restrictedToMinimumLevel = restrictedToMinimumLevel;
		}

		public void Enrich( LogEvent logEvent, ILogEventPropertyFactory propertyFactory )
		{
			if ( logEvent.Level < _restrictedToMinimumLevel )
			{
				return;
			}
			
			if ( _httpContextAccessor?.HttpContext == null )
			{
				return;
			}
			
			//todo: How to get the RaygunRequestMessageOptions
			RaygunRequestMessage httpRequestMessage = RaygunAspNetCoreRequestMessageBuilder
				.Build( _httpContextAccessor.HttpContext, new RaygunRequestMessageOptions( ) )
				.GetAwaiter()
				.GetResult();
			
			RaygunResponseMessage httpResponseMessage = RaygunAspNetCoreResponseMessageBuilder.Build( _httpContextAccessor.HttpContext );

			// The Raygun request/response messages are stored in the logEvent properties collection.
			// When the error is sent to Raygun, these messages are extracted from the known properties
			// and then removed so as to not duplicate data in the payload.
			logEvent.AddPropertyIfAbsent( propertyFactory.CreateProperty( RaygunRequestMessagePropertyName, httpRequestMessage, true ) );
			logEvent.AddPropertyIfAbsent( propertyFactory.CreateProperty( RaygunResponseMessagePropertyName, httpResponseMessage, true ) );
		}
	}
}
#endif