using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WebSocketSharp.Owin.RequestProcessing;
using HttpListenerContext = Gottscj.Owin.Host.SocketHttpListener.Net.HttpListenerContext;
using HttpListenerRequest = Gottscj.Owin.Host.SocketHttpListener.Net.HttpListenerRequest;

namespace Gottscj.Owin.Host.RequestProcessing
{
    /// <summary>
    /// This wraps an HttpListenerRequest and exposes it as an OWIN environment IDictionary.
    /// </summary>
    internal class OwinHttpListenerRequest
    {
        private readonly CallEnvironment _environment;
        private readonly HttpListenerContext _context;
        private readonly HttpListenerRequest _request;

        /// <summary>
        /// Initializes a new instance of the <see cref="OwinHttpListenerRequest"/> class.
        /// Uses the given request object to populate the OWIN standard keys in the environment IDictionary.
        /// Most values are copied so that they can be mutable, but the headers collection is only wrapped.
        /// </summary>
        internal OwinHttpListenerRequest(HttpListenerContext context, string basePath, string path, string query, CallEnvironment environment)
        {
            Contract.Requires(context != null);

            _context = context;
            _request = context.Request;
            _environment = environment;

            _environment.RequestProtocol = GetProtocol(_request.ProtocolVersion);
            _environment.RequestScheme = _request.IsSecureConnection ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            _environment.RequestMethod = _request.HttpMethod;
            _environment.RequestPathBase = basePath;
            _environment.RequestPath = path;
            _environment.RequestQueryString = query;
            //_environment.RequestId = request.RequestTraceIdentifier.ToString();

            _environment.RequestHeaders = new RequestHeadersDictionary(_request);

            if (_request.IsSecureConnection)
            {
                // TODO: Add delay sync load for folks that directly access the client cert key
                _environment.LoadClientCert = (Func<Task>)LoadClientCert;
            }
        }

        private static string GetProtocol(Version version)
        {
            if (version.Major == 1)
            {
                if (version.Minor == 1)
                {
                    return "HTTP/1.1";
                }
                else if (version.Minor == 0)
                {
                    return "HTTP/1.0";
                }
            }
            return "HTTP/" + version.ToString(2);
        }

        internal bool TryGetClientCert(ref X509Certificate value, ref Exception errors)
        {
            if (!_request.IsSecureConnection)
            {
                return false;
            }

            try
            {
                value = _context.Request.GetClientCertificate();
                /*if (_request..ClientCertificateError != 0)
                {
                    errors = new Win32Exception(_request.ClientCertificateError);
                }*/
                return value != null;
            }
            catch (HttpListenerException)
            {
                // TODO: LOG
                return false;
            }
        }

        private Task LoadClientCert()
        {
            try
            {
                if (!_environment.ClientCertNeedsInit)
                {
                    return Task.CompletedTask;
                }

                X509Certificate cert = _request.GetClientCertificate();

                _environment.ClientCert = cert;
                _environment.ClientCertErrors = null;
                /*_environment.ClientCertErrors =
                    (_request.ClientCertificateError == 0) ? null
                        : new Win32Exception(_request.ClientCertificateError);*/
            }
            catch (Exception)
            {
                _environment.ClientCert = null;
                _environment.ClientCertErrors = null;
                // TODO: LOG
            }
            return Task.CompletedTask;
        }

        internal Stream GetRequestBody()
        {
            return new HttpListenerStreamWrapper(_request.InputStream);
        }

        internal string GetRemoteIpAddress()
        {
            return _request.RemoteEndPoint.Address.ToString();
        }

        internal string GetRemotePort()
        {
            return _request.RemoteEndPoint.Port.ToString(CultureInfo.InvariantCulture);
        }

        internal string GetLocalIpAddress()
        {
            return _request.LocalEndPoint.Address.ToString();
        }

        internal string GetLocalPort()
        {
            return _request.LocalEndPoint.Port.ToString(CultureInfo.InvariantCulture);
        }

        internal bool GetIsLocal()
        {
            return _request.IsLocal;
        }
    }
}