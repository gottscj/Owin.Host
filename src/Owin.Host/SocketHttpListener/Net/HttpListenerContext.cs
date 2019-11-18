using System;
using System.Net;
using System.Security.Principal;
using Gottscj.Owin.Host.SocketHttpListener.Net.WebSockets;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    public sealed class HttpListenerContext
    {
        private readonly HttpListenerRequest _request;
        private readonly HttpListenerResponse _response;
        private IPrincipal _user;
        private readonly HttpConnection _cnc;
        private string _error;
        private int _errStatus = 400;
        internal HttpListener Listener;
        private readonly ILogger _logger;

        internal HttpListenerContext(HttpConnection cnc, ILogger logger)
        {
            _cnc = cnc;
            _logger = logger;
            _request = new HttpListenerRequest(this);
            _response = new HttpListenerResponse(this, _logger);
        }

        internal int ErrorStatus
        {
            get => _errStatus;
            set => _errStatus = value;
        }

        internal string ErrorMessage
        {
            get => _error;
            set => _error = value;
        }

        internal bool HaveError => (_error != null);

        internal HttpConnection Connection => _cnc;

        public HttpListenerRequest Request => _request;

        public HttpListenerResponse Response => _response;

        public IPrincipal User => _user;

        internal void ParseAuthentication(AuthenticationSchemes expectedSchemes)
        {
            if (expectedSchemes == AuthenticationSchemes.Anonymous)
                return;

            // TODO: Handle NTLM/Digest modes
            string header = _request.Headers["Authorization"];
            if (header == null || header.Length < 2)
                return;

            string[] authenticationData = header.Split(new char[] { ' ' }, 2);
            if (string.Compare(authenticationData[0], "basic", true) == 0)
            {
                _user = ParseBasicAuthentication(authenticationData[1]);
            }
            // TODO: throw if malformed -> 400 bad request
        }

        internal IPrincipal ParseBasicAuthentication(string authData)
        {
            try
            {
                // Basic AUTH Data is a formatted Base64 String
                //string domain = null;
                string user = null;
                string password = null;
                int pos = -1;
                string authString = System.Text.Encoding.Default.GetString(Convert.FromBase64String(authData));

                // The format is DOMAIN\username:password
                // Domain is optional

                pos = authString.IndexOf(':');

                // parse the password off the end
                password = authString.Substring(pos + 1);

                // discard the password
                authString = authString.Substring(0, pos);

                // check if there is a domain
                pos = authString.IndexOf('\\');

                if (pos > 0)
                {
                    //domain = authString.Substring (0, pos);
                    user = authString.Substring(pos);
                }
                else
                {
                    user = authString;
                }

                HttpListenerBasicIdentity identity = new HttpListenerBasicIdentity(user, password);
                // TODO: What are the roles MS sets
                return new GenericPrincipal(identity, new string[0]);
            }
            catch (Exception)
            {
                // Invalid auth data is swallowed silently
                return null;
            }
        }

        public HttpListenerWebSocketContext AcceptWebSocket(string protocol)
        {
            if (protocol != null)
            {
                if (protocol.Length == 0)
                    throw new ArgumentException("An empty string.", nameof(protocol));

                if (!protocol.IsToken())
                    throw new ArgumentException("Contains an invalid character.", nameof(protocol));
            }

            return new HttpListenerWebSocketContext(this, protocol);
        }
    }
}
