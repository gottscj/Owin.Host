using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    internal sealed class EndPointListener
    {
        private readonly HttpListener _listener;
        private readonly IPEndPoint _endpoint;
        private Socket _sock;
        private Dictionary<ListenerPrefix,HttpListener> _prefixes;  // Dictionary <ListenerPrefix, HttpListener>
        private List<ListenerPrefix> _unhandled; // List<ListenerPrefix> unhandled; host = '*'
        private List<ListenerPrefix> _all;       // List<ListenerPrefix> all;  host = '+'
        private readonly X509Certificate _cert;
        private readonly bool _secure;
        private readonly Dictionary<HttpConnection, HttpConnection> _unregistered;
        private readonly ILogger _logger;
        private bool _closed;
        private readonly bool _enableDualMode;

        public EndPointListener(HttpListener listener, IPAddress addr, int port, bool secure, X509Certificate cert, ILogger logger)
        {
            _listener = listener;
            _logger = logger;

            _secure = secure;
            _cert = cert;

            _enableDualMode = Equals(addr, IPAddress.IPv6Any);
            _endpoint = new IPEndPoint(addr, port);

            _prefixes = new Dictionary<ListenerPrefix, HttpListener>();
            _unregistered = new Dictionary<HttpConnection, HttpConnection>();

            CreateSocket();
        }

        internal HttpListener Listener => _listener;

        private void CreateSocket()
        {
            if (_enableDualMode)
            {
                _logger.Info("Enabling DualMode socket");

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    _sock = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    EnableDualMode(_sock);
                }
                else
                {
                    _sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }
            }
            else
            {
                _logger.Info("Enabling non-DualMode socket");
                _sock = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }

            _sock.Bind(_endpoint);

            // This is the number TcpListener uses.
            _sock.Listen(2147483647);

            Socket dummy = null;
            StartAccept(null, ref dummy);
            _closed = false;
        }

        private void EnableDualMode(Socket socket)
        {
            try
            {
                //sock.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);

                socket.DualMode = true;
            }
            catch (MissingMemberException)
            {
            }
        }

        public void StartAccept(SocketAsyncEventArgs acceptEventArg, ref Socket accepted)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            try
            {
                bool willRaiseEvent = _sock.AcceptAsync(acceptEventArg);

                if (!willRaiseEvent)
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch
            {
                if (accepted != null)
                {
                    try
                    {
                        accepted.Close();
                    }
                    catch
                    {
                    }
                    accepted = null;
                }
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync  
        // operations and is invoked when an accept operation is complete 
        // 
        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (_closed)
            {
                return;
            }

            // http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.acceptasync%28v=vs.110%29.aspx
            // Under certain conditions ConnectionReset can occur
            // Need to attept to re-accept
            if (e.SocketError == SocketError.ConnectionReset)
            {
                _logger.Error("SocketError.ConnectionReset reported. Attempting to re-accept.");
                Socket dummy = null;
                StartAccept(e, ref dummy);
                return;
            }

            var acceptSocket = e.AcceptSocket;
            if (acceptSocket != null)
            {
                ProcessAccept(acceptSocket);
            }

            if (_sock != null)
            {
                // Accept the next connection request
                StartAccept(e, ref acceptSocket);
            }
        }

        private void ProcessAccept(Socket accepted)
        {
            try
            {
                var listener = this;

                if (listener._secure && listener._cert == null)
                {
                    accepted.Close();
                    return;
                }

                HttpConnection conn = new HttpConnection(_logger, accepted, listener, listener._secure, listener._cert);
                //_logger.Debug("Adding unregistered connection to {0}. Id: {1}", accepted.RemoteEndPoint, connectionId);
                lock (listener._unregistered)
                {
                    listener._unregistered[conn] = conn;
                }
                conn.BeginReadRequest();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in ProcessAccept", ex);
            }
        }

        internal void RemoveConnection(HttpConnection conn)
        {
            lock (_unregistered)
            {
                _unregistered.Remove(conn);
            }
        }

        public bool BindContext(HttpListenerContext context)
        {
            HttpListenerRequest req = context.Request;
            ListenerPrefix prefix;
            HttpListener listener = SearchListener(req.Url, out prefix);
            if (listener == null)
                return false;

            context.Listener = listener;
            context.Connection.Prefix = prefix;
            return true;
        }

        public void UnbindContext(HttpListenerContext context)
        {
            if (context == null || context.Request == null)
                return;

            context.Listener.UnregisterContext(context);
        }

        private HttpListener SearchListener(Uri uri, out ListenerPrefix prefix)
        {
            prefix = null;
            if (uri == null)
                return null;

            string host = uri.Host;
            int port = uri.Port;
            string path = WebUtility.UrlDecode(uri.AbsolutePath);
            string pathSlash = path[path.Length - 1] == '/' ? path : path + "/";

            HttpListener bestMatch = null;
            int bestLength = -1;

            if (host != null && host != "")
            {
                var pRo = _prefixes;
                foreach (ListenerPrefix p in pRo.Keys)
                {
                    string ppath = p.Path;
                    if (ppath.Length < bestLength)
                        continue;

                    if (p.Host != host || p.Port != port)
                        continue;

                    if (path.StartsWith(ppath) || pathSlash.StartsWith(ppath))
                    {
                        bestLength = ppath.Length;
                        bestMatch = (HttpListener)pRo[p];
                        prefix = p;
                    }
                }
                if (bestLength != -1)
                    return bestMatch;
            }

            List<ListenerPrefix> list = _unhandled;
            bestMatch = MatchFromList(host, path, list, out prefix);
            if (path != pathSlash && bestMatch == null)
                bestMatch = MatchFromList(host, pathSlash, list, out prefix);
            if (bestMatch != null)
                return bestMatch;

            list = _all;
            bestMatch = MatchFromList(host, path, list, out prefix);
            if (path != pathSlash && bestMatch == null)
                bestMatch = MatchFromList(host, pathSlash, list, out prefix);
            if (bestMatch != null)
                return bestMatch;

            return null;
        }

        private HttpListener MatchFromList(string host, string path, List<ListenerPrefix> list, out ListenerPrefix prefix)
        {
            prefix = null;
            if (list == null)
                return null;

            HttpListener bestMatch = null;
            int bestLength = -1;

            foreach (ListenerPrefix p in list)
            {
                string ppath = p.Path;
                if (ppath.Length < bestLength)
                    continue;

                if (path.StartsWith(ppath))
                {
                    bestLength = ppath.Length;
                    bestMatch = p.Listener;
                    prefix = p;
                }
            }

            return bestMatch;
        }

        private void AddSpecial(List<ListenerPrefix> coll, ListenerPrefix prefix)
        {
            if (coll == null)
                return;

            foreach (ListenerPrefix p in coll)
            {
                if (p.Path == prefix.Path) //TODO: code
                    throw new HttpListenerException(400, "Prefix already in use.");
            }
            coll.Add(prefix);
        }

        private bool RemoveSpecial(List<ListenerPrefix> coll, ListenerPrefix prefix)
        {
            if (coll == null)
                return false;

            int c = coll.Count;
            for (int i = 0; i < c; i++)
            {
                ListenerPrefix p = (ListenerPrefix)coll[i];
                if (p.Path == prefix.Path)
                {
                    coll.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private void CheckIfRemove()
        {
            if (_prefixes.Count > 0)
                return;

            List<ListenerPrefix> list = _unhandled;
            if (list != null && list.Count > 0)
                return;

            list = _all;
            if (list != null && list.Count > 0)
                return;

            EndPointManager.RemoveEndPoint(this, _endpoint);
        }

        public void Close()
        {
            _closed = true;
            _sock.Close();
            lock (_unregistered)
            {
                //
                // Clone the list because RemoveConnection can be called from Close
                //
                var connections = new List<HttpConnection>(_unregistered.Keys);

                foreach (HttpConnection c in connections)
                    c.Close(true);
                _unregistered.Clear();
            }
        }

        public void AddPrefix(ListenerPrefix prefix, HttpListener listener)
        {
            List<ListenerPrefix> current;
            List<ListenerPrefix> future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandled;
                    future = (current != null) ? current.ToList() : new List<ListenerPrefix>();
                    prefix.Listener = listener;
                    AddSpecial(future, prefix);
                } while (Interlocked.CompareExchange(ref _unhandled, future, current) != current);
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _all;
                    future = (current != null) ? current.ToList() : new List<ListenerPrefix>();
                    prefix.Listener = listener;
                    AddSpecial(future, prefix);
                } while (Interlocked.CompareExchange(ref _all, future, current) != current);
                return;
            }

            Dictionary<ListenerPrefix, HttpListener> prefs;
            Dictionary<ListenerPrefix, HttpListener> p2;
            do
            {
                prefs = _prefixes;
                if (prefs.ContainsKey(prefix))
                {
                    HttpListener other = (HttpListener)prefs[prefix];
                    if (other != listener) // TODO: code.
                        throw new HttpListenerException(400, "There's another listener for " + prefix);
                    return;
                }
                p2 = new Dictionary<ListenerPrefix, HttpListener>(prefs);
                p2[prefix] = listener;
            } while (Interlocked.CompareExchange(ref _prefixes, p2, prefs) != prefs);
        }

        public void RemovePrefix(ListenerPrefix prefix, HttpListener listener)
        {
            List<ListenerPrefix> current;
            List<ListenerPrefix> future;
            if (prefix.Host == "*")
            {
                do
                {
                    current = _unhandled;
                    future = (current != null) ? current.ToList() : new List<ListenerPrefix>();
                    if (!RemoveSpecial(future, prefix))
                        break; // Prefix not found
                } while (Interlocked.CompareExchange(ref _unhandled, future, current) != current);
                CheckIfRemove();
                return;
            }

            if (prefix.Host == "+")
            {
                do
                {
                    current = _all;
                    future = (current != null) ? current.ToList() : new List<ListenerPrefix>();
                    if (!RemoveSpecial(future, prefix))
                        break; // Prefix not found
                } while (Interlocked.CompareExchange(ref _all, future, current) != current);
                CheckIfRemove();
                return;
            }

            Dictionary<ListenerPrefix, HttpListener> prefs;
            Dictionary<ListenerPrefix, HttpListener> p2;
            do
            {
                prefs = _prefixes;
                if (!prefs.ContainsKey(prefix))
                    break;

                p2 = new Dictionary<ListenerPrefix, HttpListener>(prefs);
                p2.Remove(prefix);
            } while (Interlocked.CompareExchange(ref _prefixes, p2, prefs) != prefs);
            CheckIfRemove();
        }
    }
}
