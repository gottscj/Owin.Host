using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebSocketSharp.Owin.RequestProcessing;
using WebSocketSharp.Owin.WebSocketSharp;
using WebSocketSharp.Owin.WebSocketSharp.Net;
using WebSocketSharp.Owin.WebSocketSharp.Server;
using LoggerFactoryFunc = System.Func<string, System.Func<System.Diagnostics.TraceEventType, int, object, System.Exception, System.Func<object, System.Exception, string>, bool>>;

namespace WebSocketSharp.Owin
{
	internal class OwinHttpServerFactory
	{
		private Func<IDictionary<string, object>, Task> _app;
		private IDictionary<string, object> _properties;
		private HttpServer _httpServer;
		private string _basePath;
		private OwinLogger _logger;
		
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

			var addresses = (IList<IDictionary<string, object>>)properties["host.Addresses"];
			if (addresses.Count != 1)
			{
				throw new InvalidOperationException($"'{nameof(OwinHost)}' only supports one url");
			}

			var address = addresses.Single();
			// build url from parts
			var scheme = address.ContainsKey("scheme") ? address["scheme"].ToString() : Uri.UriSchemeHttp;
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

			_basePath = path;
			// add a server for each url
			var url = scheme + "://" + host + port + path;
			
			_httpServer = new HttpServer(url);
			_httpServer.OnRequest += ProcessRequest;
			_httpServer.Start();
			
			return new Disposable(_httpServer);
		}

		private void ProcessRequest(object sender, HttpRequestEventArgs e)
		{
			try
			{
				var context = e.Context;
				GetPathAndQuery(context.Request, out var pathBase, out var path, out var query);
				var owinContext = new OwinHttpListenerContext(context, pathBase, path, query);
				_app.Invoke(owinContext.Environment).GetAwaiter().GetResult();
			}
			catch (Exception exception)
			{
				HandleRequestError(exception);
				e.Response.StatusCode = 500; // internal server error
				e.Response.ContentEncoding = Encoding.UTF8;
				e.Response.ContentType = "application/json";
//				e.Response.AppendHeader("Content-Type", "application/json");
				var errorObject = new Dictionary<string, object>
				{
					["Code"] = exception.GetType().Name.Replace(nameof(Exception), ""),
					["Message"] = exception.Message,
					["Details"] = exception.ToString()
				};
				var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(errorObject));
				e.Response.WriteContent(buffer);
			}
		}

		// When the server is listening on multiple urls, we need to decide which one is the correct base path for this request.
		private void GetPathAndQuery(HttpListenerRequest request, out string pathBase, out string path,
			out string query)
		{
			var cookedPath = "/" + request.Url.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);
			query = request.Url.Query;


			if (!string.IsNullOrEmpty(query) && query[0] == '?')
			{
				query = query.Substring(1); // Drop the ?
			}

			// Find the split between path and pathBase.
			// This will only do full segment path matching because all _basePaths end in a '/'.
			bool endsInSlash = true;
			string bestMatch = "/";

			if (_basePath.Length > bestMatch.Length)
			{
				if (_basePath.Length <= cookedPath.Length
				    && cookedPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
				{
					bestMatch = _basePath;
					endsInSlash = true;
				}
				else if (_basePath.Length == cookedPath.Length + 1
				         && string.Compare(_basePath, 0, cookedPath, 0, cookedPath.Length,
					         StringComparison.OrdinalIgnoreCase) == 0)
				{
					// They matched exactly except for the trailing slash.
					bestMatch = _basePath;
					endsInSlash = false;
				}
			}


			// pathBase must be empty or start with a slash and not end with a slash (/pathBase)
			// path must start with a slash (/path)
			if (endsInSlash)
			{
				// Move the matched '/' from the end of the pathBase to the start of the path.
				pathBase = cookedPath.Substring(0, bestMatch.Length - 1);
				path = cookedPath.Substring(bestMatch.Length - 1);
			}
			else
			{
				pathBase = cookedPath;
				path = string.Empty;
			}
		}

		private void HandleRequestError(Exception exception)
		{
			// StartNextRequestAsync should handle it's own exceptions.
			_logger.Exception("Unexpected exception.", exception);
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Un-expected exception path: " + exception.ToString());
			Console.ResetColor();
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