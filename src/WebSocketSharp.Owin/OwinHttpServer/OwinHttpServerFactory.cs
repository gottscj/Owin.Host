using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using LoggerFactoryFunc = System.Func<string, System.Func<System.Diagnostics.TraceEventType, int, object, System.Exception, System.Func<object, System.Exception, string>, bool>>;
using GeContextFunc = System.Func<WebSocketSharp.Server.HttpRequestEventArgs, WebSocketSharp.Net.HttpListenerContext>;

namespace Shure.Cwb.WebApi.Service.OwinHttpServer
{
	internal class OwinHttpServerFactory
	{
		private Func<IDictionary<string, object>, Task> _app;
		private IDictionary<string, object> _properties;
		private HttpServer _httpServer;
		private OwinLogger _logger;
		private GeContextFunc _getContextFunc;
		public OwinHttpServerFactory()
		{
			
		}
		/// <summary>
		/// Called by <see cref="Microsoft.Owin.Hosting.ServerFactory.ServerFactoryAdapter"/>
		/// which starts the server
		/// </summary>
		/// <param name="app"></param>
		/// <param name="properties"></param>
		/// <returns></returns>
		public IDisposable Create(Func<IDictionary<string, object>, Task> app, IDictionary<string, object> properties)
		{
			
			_app = app;
			_properties = properties;

			var loggerFactory = (LoggerFactoryFunc)properties["server.LoggerFactory"];
			_logger = new OwinLogger(loggerFactory, GetType());
			
			var instExp = Expression.Parameter(typeof(HttpRequestEventArgs));
			var fieldInfo =
				typeof(HttpRequestEventArgs).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
			
			if (fieldInfo == null)
			{
				throw new InvalidOperationException($"Expected private field '_context' to be present in WebSocketSharp.Server.HttpRequestEventArgs\r\n" +
				                                    $"Go to https://github.com/sta/websocket-sharp and check field name");
			}
			
			var fieldExp = Expression.Field(instExp, fieldInfo);
			_getContextFunc = Expression.Lambda<Func<HttpRequestEventArgs, HttpListenerContext>>(fieldExp, instExp).Compile();
			
			var addresses = (IList<IDictionary<string, object>>)properties["host.Addresses"];
			if (addresses.Count != 1)
			{
				throw new InvalidOperationException($"'{nameof(OwinHttpServer)}' only supports one url");
			}

			var address = addresses.Single();
			// build url from parts
			var scheme = address.ContainsKey("scheme") ? address["Scheme"].ToString() : Uri.UriSchemeHttp;
			var host = address.ContainsKey("host") ?  address["host"].ToString() : "localhost";
			var port = address.ContainsKey("port") ? address["port"].ToString() : "5000";
			var path = address.ContainsKey("path")? address["path"].ToString() : string.Empty;
				
			// if port is present, add delimiter to value before concatenation
			if (!string.IsNullOrWhiteSpace(port))
			{
				port = ":" + port;
			}
			// Assume http(s)://+:9090/BasePath/, including the first path slash.  May be empty. Must end with a slash.
			if (!path.EndsWith("/", StringComparison.Ordinal))
			{
				// Http.Sys requires that the URL end in a slash
				path += "/";
			}
			// add a server for each url
			var url = scheme + "://" + host + port + path;
			
			_httpServer = new HttpServer(url);
			_httpServer.OnDelete += ProcessRequestAsync;
			_httpServer.OnGet += ProcessRequestAsync;
			_httpServer.OnPatch += ProcessRequestAsync;
			_httpServer.OnPost += ProcessRequestAsync;
			_httpServer.OnPut += ProcessRequestAsync;
			_httpServer.Start();
			
			return new Disposable(_httpServer);
		}

		
		private async void ProcessRequestAsync(object sender, HttpRequestEventArgs e)
		{
			HttpListenerContext context = null;
			try
			{

				context = _getContextFunc(e);
				
				var owinContext = new OwinHttpListenerContext();
				await _app.Invoke(env);
				
			}
			catch (Exception exception)
			{
				HandleRequestError(exception);
			}
		}

		private void HandleRequestError(Exception exception)
		{
			// StartNextRequestAsync should handle it's own exceptions.
			_logger.Exception("Unexpected exception.", exception);
			Contract.Assert(false, "Un-expected exception path: " + exception.ToString());
#if DEBUG
			// Break into the debugger in case the message pump fails.
			System.Diagnostics.Debugger.Break();
#endif
		}

		public Task Next(IDictionary<string, object> env)
		{
			return 
		}

		private class Disposable : IDisposable
		{
			private readonly HttpServer _httpServer;

			public Disposable(HttpServer httpServer)
			{
				_httpServer = httpServer;
			}
			public void Dispose()
			{
				_httpServer.Stop(CloseStatusCode.Normal, "Server shutting downs");
			}
		}
	}
}