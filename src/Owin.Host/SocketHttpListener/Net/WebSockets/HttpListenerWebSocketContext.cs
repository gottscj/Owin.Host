using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Principal;

namespace Gottscj.Owin.Host.SocketHttpListener.Net.WebSockets
{
    /// <summary>
    /// Provides the properties used to access the information in a WebSocket connection request
    /// received by the <see cref="HttpListener"/>.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public class HttpListenerWebSocketContext : WebSocketContext
    {
        #region Private Fields

        private readonly HttpListenerContext _context;
        private readonly WebSocket _websocket;

        #endregion

        #region Internal Constructors

        internal HttpListenerWebSocketContext(
          HttpListenerContext context, string protocol)
        {
            _context = context;
            _websocket = new WebSocket(this, protocol);
        }

        #endregion

        #region Internal Properties

        internal Stream Stream => _context.Connection.Stream;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the HTTP cookies included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.CookieCollection"/> that contains the cookies.
        /// </value>
        public override CookieCollection CookieCollection => _context.Request.Cookies;

        /// <summary>
        /// Gets the HTTP headers included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the headers.
        /// </value>
        public override NameValueCollection Headers => _context.Request.Headers;

        /// <summary>
        /// Gets the value of the Host header included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Host header.
        /// </value>
        public override string Host => _context.Request.Headers["Host"];

        /// <summary>
        /// Gets a value indicating whether the client is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
        /// </value>
        public override bool IsAuthenticated => _context.Request.IsAuthenticated;

        /// <summary>
        /// Gets a value indicating whether the client connected from the local computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client connected from the local computer; otherwise, <c>false</c>.
        /// </value>
        public override bool IsLocal => _context.Request.IsLocal;

        /// <summary>
        /// Gets a value indicating whether the WebSocket connection is secured.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secured; otherwise, <c>false</c>.
        /// </value>
        public override bool IsSecureConnection => _context.Connection.IsSecure;

        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket connection request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket connection request; otherwise, <c>false</c>.
        /// </value>
        public override bool IsWebSocketRequest => _context.Request.IsWebSocketRequest;

        /// <summary>
        /// Gets the value of the Origin header included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Origin header.
        /// </value>
        public override string Origin => _context.Request.Headers["Origin"];

        /// <summary>
        /// Gets the query string included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the query string parameters.
        /// </value>
        public override NameValueCollection QueryString => _context.Request.QueryString;

        /// <summary>
        /// Gets the URI requested by the client.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> that represents the requested URI.
        /// </value>
        public override Uri RequestUri => _context.Request.Url;

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Key header included in the request.
        /// </summary>
        /// <remarks>
        /// This property provides a part of the information used by the server to prove that it
        /// received a valid WebSocket connection request.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Sec-WebSocket-Key header.
        /// </value>
        public override string SecWebSocketKey => _context.Request.Headers["Sec-WebSocket-Key"];

        /// <summary>
        /// Gets the values of the Sec-WebSocket-Protocol header included in the request.
        /// </summary>
        /// <remarks>
        /// This property represents the subprotocols requested by the client.
        /// </remarks>
        /// <value>
        /// An <see cref="T:System.Collections.Generic.IEnumerable{string}"/> instance that provides
        /// an enumerator which supports the iteration over the values of the Sec-WebSocket-Protocol
        /// header.
        /// </value>
        public override IEnumerable<string> SecWebSocketProtocols
        {
            get
            {
                var protocols = _context.Request.Headers["Sec-WebSocket-Protocol"];
                if (protocols != null)
                    foreach (var protocol in protocols.Split(','))
                        yield return protocol.Trim();
            }
        }

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Version header included in the request.
        /// </summary>
        /// <remarks>
        /// This property represents the WebSocket protocol version.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Sec-WebSocket-Version header.
        /// </value>
        public override string SecWebSocketVersion => _context.Request.Headers["Sec-WebSocket-Version"];

        /// <summary>
        /// Gets the server endpoint as an IP address and a port number.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the server endpoint.
        /// </value>
        public override IPEndPoint ServerEndPoint => _context.Connection.LocalEndPoint;

        /// <summary>
        /// Gets the client information (identity, authentication, and security roles).
        /// </summary>
        /// <value>
        /// A <see cref="IPrincipal"/> that represents the client information.
        /// </value>
        public override IPrincipal User => _context.User;

        /// <summary>
        /// Gets the client endpoint as an IP address and a port number.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the client endpoint.
        /// </value>
        public override IPEndPoint UserEndPoint => _context.Connection.RemoteEndPoint;

        /// <summary>
        /// Gets the <see cref="SocketHttpListener.WebSocket"/> instance used for two-way communication
        /// between client and server.
        /// </summary>
        /// <value>
        /// A <see cref="SocketHttpListener.WebSocket"/>.
        /// </value>
        public override WebSocket WebSocket => _websocket;

        #endregion

        #region Internal Methods

        internal void Close()
        {
            try
            {
                _context.Connection.Close(true);
            }
            catch
            {
                // catch errors sending the closing handshake
            }
        }

        internal void Close(HttpStatusCode code)
        {
            _context.Response.Close(code);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current
        /// <see cref="HttpListenerWebSocketContext"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current
        /// <see cref="HttpListenerWebSocketContext"/>.
        /// </returns>
        public override string ToString()
        {
            return _context.Request.ToString();
        }

        #endregion
    }
}
