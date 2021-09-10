using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mindscape.Raygun4Net;

#if NETSTANDARD2_0
using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net.AspNetCore;
#endif

namespace Serilog.Sinks.Raygun
{
	public class SerilogRaygunClient : RaygunClient
	{
#if NETSTANDARD2_0
		private readonly ThreadLocal<RaygunRequestMessage> _currentRequestMessage = new ThreadLocal<RaygunRequestMessage>(() => null);
		private readonly ThreadLocal<RaygunResponseMessage> _currentResponseMessage = new ThreadLocal<RaygunResponseMessage>(() => null);
		
		public SerilogRaygunClient( string apiKey ) : base (apiKey)
		{ }
		
		public SerilogRaygunClient( RaygunSettings settings, HttpContext context = null ) : base( settings, context )
		{ }

		public void SetHttpRequestMessages( RaygunRequestMessage httpRequestMessage, RaygunResponseMessage httpResponseMessage )
		{
			_currentRequestMessage.Value = httpRequestMessage;
			_currentResponseMessage.Value = httpResponseMessage;
		}

		protected override async Task<RaygunMessage> BuildMessage( Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfoMessage )
		{
			RaygunMessage message = await base.BuildMessage( exception, tags, userCustomData, userInfoMessage );

			if ( _currentRequestMessage.Value != null )
			{
				message.Details.Request = _currentRequestMessage.Value;
			}
			if ( _currentResponseMessage.Value != null )
			{
				message.Details.Response = _currentResponseMessage.Value;
			}

			return message;
		}
#else
		public SerilogRaygunClient( string apiKey ) : base ( string.IsNullOrWhiteSpace(apiKey) ? RaygunSettings.Settings.ApiKey : apiKey )
		{ }
#endif
	}
}