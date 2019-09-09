using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using WebSocketSharp.Owin.WebSocketSharp.Net;

namespace WebSocketSharp.Owin.RequestProcessing
{
    internal class OwinHttpListenerContext : IDisposable, CallEnvironment.IPropertySource
    {
        private readonly OwinHttpListenerRequest _owinRequest;
        private readonly OwinHttpListenerResponse _owinResponse;
        private readonly CallEnvironment _environment;

        private IPrincipal _user;

        internal OwinHttpListenerContext(HttpListenerContext context, string basePath, string path, string query)
        {
            _environment = new CallEnvironment(this);
            
            _owinRequest = new OwinHttpListenerRequest(context, basePath, path, query, _environment);
            _owinResponse = new OwinHttpListenerResponse(context, _environment);

            _environment.OwinVersion = Constants.OwinVersion;

            SetServerUser(context.User);
            _environment.RequestContext = context;
        }

        internal CallEnvironment Environment => _environment;

        internal OwinHttpListenerRequest Request => _owinRequest;

        internal OwinHttpListenerResponse Response => _owinResponse;

        internal void End(Exception ex)
        {
            if (ex != null)
            {
                // TODO: LOG
                // Lazy initialized
            }

            End();
        }

        internal void End()
        {
            _owinResponse.End();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        // Lazy environment initialization

        public CancellationToken GetCallCancelled()
        {
            return CancellationToken.None;
        }

        public Stream GetRequestBody()
        {
            return _owinRequest.GetRequestBody();
        }

        public string GetServerRemoteIpAddress()
        {
            return _owinRequest.GetRemoteIpAddress();
        }

        public string GetServerRemotePort()
        {
            return _owinRequest.GetRemotePort();
        }

        public string GetServerLocalIpAddress()
        {
            return _owinRequest.GetLocalIpAddress();
        }

        public string GetServerLocalPort()
        {
            return _owinRequest.GetLocalPort();
        }

        public bool GetServerIsLocal()
        {
            return _owinRequest.GetIsLocal();
        }

        public IPrincipal GetServerUser()
        {
            return _user;
        }

        public void SetServerUser(IPrincipal user)
        {
            _user = user;
            Thread.CurrentPrincipal = _user;
        }

        public bool TryGetClientCert(ref X509Certificate value)
        {
            Exception clientCertErrors = null;
            bool result = _owinRequest.TryGetClientCert(ref value, ref clientCertErrors);
            Environment.ClientCertErrors = clientCertErrors;
            return result;
        }

        public bool TryGetClientCertErrors(ref Exception value)
        {
            X509Certificate clientCert = null;
            bool result = _owinRequest.TryGetClientCert(ref clientCert, ref value);
            Environment.ClientCert = clientCert;
            return result;
        }
    }
}