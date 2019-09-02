using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Shure.Cwb.WebApi.Service.OwinHttpServer.RequestProcessing
{
	using WebSocketAccept =
		Action<IDictionary<string, object>, // WebSocket Accept parameters
			Func<IDictionary<string, object>, // WebSocket environment
				Task /* Complete */>>;
	
	internal interface IPropertySource
	{
		Stream GetRequestBody();
		CancellationToken GetCallCancelled();
		IPrincipal GetServerUser();
		void SetServerUser(IPrincipal value);
		string GetServerRemoteIpAddress();
		string GetServerRemotePort();
		string GetServerLocalIpAddress();
		string GetServerLocalPort();
		bool GetServerIsLocal();
		bool TryGetClientCert(ref X509Certificate value);
		bool TryGetClientCertErrors(ref Exception value);
		bool TryGetWebSocketAccept(ref WebSocketAccept value);
	}
}