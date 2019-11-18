using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    public sealed class HttpListenerRequest
    {
        private string[] _acceptTypes;
        private Encoding _contentEncoding;
        private long _contentLength;
        private bool _clSet;
        private CookieCollection _cookies;
        private readonly WebHeaderCollection _headers;
        private string _method;
        private Stream _inputStream;
        private Version _version;
        private NameValueCollection _queryString; // check if null is ok, check if read-only, check case-sensitiveness
        private string _rawUrl;
        private Uri _url;
        private Uri _referrer;
        private string[] _userLanguages;
        private readonly HttpListenerContext _context;
        private bool _isChunked;
        private bool _kaSet;
        private bool _keepAlive;

        private delegate X509Certificate2 GccDelegate();

        private GccDelegate _gccDelegate;

        private static readonly byte[] _100Continue = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");

        internal HttpListenerRequest(HttpListenerContext context)
        {
            _context = context;
            _headers = new WebHeaderCollection();
            _version = HttpVersion.Version10;
        }

        private static readonly char[] Separators = new char[] { ' ' };

        internal void SetRequestLine(string req)
        {
            string[] parts = req.Split(Separators, 3);
            if (parts.Length != 3)
            {
                _context.ErrorMessage = "Invalid request line (parts).";
                return;
            }

            _method = parts[0];
            foreach (char c in _method)
            {
                int ic = (int)c;

                if ((ic >= 'A' && ic <= 'Z') ||
                    (ic > 32 && c < 127 && c != '(' && c != ')' && c != '<' &&
                     c != '<' && c != '>' && c != '@' && c != ',' && c != ';' &&
                     c != ':' && c != '\\' && c != '"' && c != '/' && c != '[' &&
                     c != ']' && c != '?' && c != '=' && c != '{' && c != '}'))
                    continue;

                _context.ErrorMessage = "(Invalid verb)";
                return;
            }

            _rawUrl = parts[1];
            if (parts[2].Length != 8 || !parts[2].StartsWith("HTTP/"))
            {
                _context.ErrorMessage = "Invalid request line (version).";
                return;
            }

            try
            {
                _version = new Version(parts[2].Substring(5));
                if (_version.Major < 1)
                    throw new Exception();
            }
            catch
            {
                _context.ErrorMessage = "Invalid request line (version).";
                return;
            }
        }

        private void CreateQueryString(string query)
        {
            if (query == null || query.Length == 0)
            {
                _queryString = new NameValueCollection(1);
                return;
            }

            _queryString = new NameValueCollection();
            if (query[0] == '?')
                query = query.Substring(1);
            string[] components = query.Split('&');
            foreach (string kv in components)
            {
                int pos = kv.IndexOf('=');
                if (pos == -1)
                {
                    _queryString.Add(null, WebUtility.UrlDecode(kv));
                }
                else
                {
                    string key = WebUtility.UrlDecode(kv.Substring(0, pos));
                    string val = WebUtility.UrlDecode(kv.Substring(pos + 1));

                    _queryString.Add(key, val);
                }
            }
        }

        internal void FinishInitialization()
        {
            string host = UserHostName;
            if (_version > HttpVersion.Version10 && (host == null || host.Length == 0))
            {
                _context.ErrorMessage = "Invalid host name";
                return;
            }

            string path;
            Uri rawUri = null;
            if (MaybeUri(_rawUrl.ToLowerInvariant()) && Uri.TryCreate(_rawUrl, UriKind.Absolute, out rawUri))
                path = rawUri.PathAndQuery;
            else
                path = _rawUrl;

            if ((host == null || host.Length == 0))
                host = UserHostAddress;

            if (rawUri != null)
                host = rawUri.Host;

            int colon = host.LastIndexOf(':');
            if (colon >= 0)
                host = host.Substring(0, colon);

            string baseUri = String.Format("{0}://{1}:{2}",
                (IsSecureConnection) ? (IsWebSocketRequest ? "wss" : "https") : (IsWebSocketRequest ? "ws" : "http"),
                                host, LocalEndPoint.Port);

            if (!Uri.TryCreate(baseUri + path, UriKind.Absolute, out _url))
            {
                _context.ErrorMessage = WebUtility.HtmlEncode("Invalid url: " + baseUri + path);
                return;
            }

            CreateQueryString(_url.Query);

            if (_version >= HttpVersion.Version11)
            {
                string tEncoding = Headers["Transfer-Encoding"];
                _isChunked = (tEncoding != null && String.Compare(tEncoding, "chunked", StringComparison.OrdinalIgnoreCase) == 0);
                // 'identity' is not valid!
                if (tEncoding != null && !_isChunked)
                {
                    _context.Connection.SendError(null, 501);
                    return;
                }
            }

            if (!_isChunked && !_clSet)
            {
                if (String.Compare(_method, "POST", StringComparison.OrdinalIgnoreCase) == 0 ||
                    String.Compare(_method, "PUT", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _context.Connection.SendError(null, 411);
                    return;
                }
            }

            if (String.Compare(Headers["Expect"], "100-continue", StringComparison.OrdinalIgnoreCase) == 0)
            {
                ResponseStream output = _context.Connection.GetResponseStream();
                output.InternalWrite(_100Continue, 0, _100Continue.Length);
            }
        }

        private static bool MaybeUri(string s)
        {
            int p = s.IndexOf(':');
            if (p == -1)
                return false;

            if (p >= 10)
                return false;

            return IsPredefinedScheme(s.Substring(0, p));
        }

        //
        // Using a simple block of if's is twice as slow as the compiler generated
        // switch statement.   But using this tuned code is faster than the
        // compiler generated code, with a million loops on x86-64:
        //
        // With "http": .10 vs .51 (first check)
        // with "https": .16 vs .51 (second check)
        // with "foo": .22 vs .31 (never found)
        // with "mailto": .12 vs .51  (last check)
        //
        //
        private static bool IsPredefinedScheme(string scheme)
        {
            if (scheme == null || scheme.Length < 3)
                return false;

            char c = scheme[0];
            if (c == 'h')
                return (scheme == "http" || scheme == "https");
            if (c == 'f')
                return (scheme == "file" || scheme == "ftp");

            if (c == 'n')
            {
                c = scheme[1];
                if (c == 'e')
                    return (scheme == "news" || scheme == "net.pipe" || scheme == "net.tcp");
                if (scheme == "nntp")
                    return true;
                return false;
            }
            if ((c == 'g' && scheme == "gopher") || (c == 'm' && scheme == "mailto"))
                return true;

            return false;
        }

        internal static string Unquote(String str)
        {
            int start = str.IndexOf('\"');
            int end = str.LastIndexOf('\"');
            if (start >= 0 && end >= 0)
                str = str.Substring(start + 1, end - 1);
            return str.Trim();
        }

        internal void AddHeader(string header)
        {
            int colon = header.IndexOf(':');
            if (colon == -1 || colon == 0)
            {
                _context.ErrorMessage = "Bad Request";
                _context.ErrorStatus = 400;
                return;
            }

            string name = header.Substring(0, colon).Trim();
            string val = header.Substring(colon + 1).Trim();
            string lower = name.ToLower(CultureInfo.InvariantCulture);
            _headers.SetInternal(name, val);
            switch (lower)
            {
                case "accept-language":
                    _userLanguages = val.Split(','); // yes, only split with a ','
                    break;
                case "accept":
                    _acceptTypes = val.Split(','); // yes, only split with a ','
                    break;
                case "content-length":
                    try
                    {
                        //TODO: max. content_length?
                        _contentLength = Int64.Parse(val.Trim());
                        if (_contentLength < 0)
                            _context.ErrorMessage = "Invalid Content-Length.";
                        _clSet = true;
                    }
                    catch
                    {
                        _context.ErrorMessage = "Invalid Content-Length.";
                    }

                    break;
                case "content-type":
                    {
                        var contents = val.Split(';');
                        foreach (var content in contents)
                        {
                            var tmp = content.Trim();
                            if (tmp.StartsWith("charset"))
                            {
                                var charset = tmp.GetValue("=");
                                if (charset != null && charset.Length > 0)
                                {
                                    try
                                    {

                                        // Support upnp/dlna devices - CONTENT-TYPE: text/xml ; charset="utf-8"\r\n
                                        charset = charset.Trim('"');
                                        var index = charset.IndexOf('"');
                                        if (index != -1) charset = charset.Substring(0, index);

                                        _contentEncoding = Encoding.GetEncoding(charset);
                                    }
                                    catch
                                    {
                                        _context.ErrorMessage = "Invalid Content-Type header: " + charset;
                                    }
                                }

                                break;
                            }
                        }
                    }
                    break;
                case "referer":
                    try
                    {
                        _referrer = new Uri(val);
                    }
                    catch
                    {
                        _referrer = new Uri("http://someone.is.screwing.with.the.headers.com/");
                    }
                    break;
                case "cookie":
                    if (_cookies == null)
                        _cookies = new CookieCollection();

                    string[] cookieStrings = val.Split(new char[] { ',', ';' });
                    Cookie current = null;
                    int version = 0;
                    foreach (string cookieString in cookieStrings)
                    {
                        string str = cookieString.Trim();
                        if (str.Length == 0)
                            continue;
                        if (str.StartsWith("$Version"))
                        {
                            version = Int32.Parse(Unquote(str.Substring(str.IndexOf('=') + 1)));
                        }
                        else if (str.StartsWith("$Path"))
                        {
                            if (current != null)
                                current.Path = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else if (str.StartsWith("$Domain"))
                        {
                            if (current != null)
                                current.Domain = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else if (str.StartsWith("$Port"))
                        {
                            if (current != null)
                                current.Port = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else
                        {
                            if (current != null)
                            {
                                _cookies.Add(current);
                            }
                            current = new Cookie();
                            int idx = str.IndexOf('=');
                            if (idx > 0)
                            {
                                current.Name = str.Substring(0, idx).Trim();
                                current.Value = str.Substring(idx + 1).Trim();
                            }
                            else
                            {
                                current.Name = str.Trim();
                                current.Value = String.Empty;
                            }
                            current.Version = version;
                        }
                    }
                    if (current != null)
                    {
                        _cookies.Add(current);
                    }
                    break;
            }
        }

        // returns true is the stream could be reused.
        internal bool FlushInput()
        {
            if (!HasEntityBody)
                return true;

            int length = 2048;
            if (_contentLength > 0)
                length = (int)Math.Min(_contentLength, (long)length);

            byte[] bytes = new byte[length];
            while (true)
            {
                // TODO: test if MS has a timeout when doing this
                try
                {
                    IAsyncResult ares = InputStream.BeginRead(bytes, 0, length, null, null);
                    if (!ares.IsCompleted && !ares.AsyncWaitHandle.WaitOne(1000))
                        return false;
                    if (InputStream.EndRead(ares) <= 0)
                        return true;
                }
                catch (ObjectDisposedException)
                {
                    _inputStream = null;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public string[] AcceptTypes => _acceptTypes;

        public int ClientCertificateError
        {
            get
            {
                HttpConnection cnc = _context.Connection;
                //if (cnc.ClientCertificate == null)
                //    throw new InvalidOperationException("No client certificate");
                //int[] errors = cnc.ClientCertificateErrors;
                //if (errors != null && errors.Length > 0)
                //    return errors[0];
                return 0;
            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                if (_contentEncoding == null)
                    _contentEncoding = Encoding.Default;
                return _contentEncoding;
            }
        }

        public long ContentLength64 => _contentLength;

        public string ContentType => _headers["content-type"];

        public CookieCollection Cookies
        {
            get
            {
                // TODO: check if the collection is read-only
                if (_cookies == null)
                    _cookies = new CookieCollection();
                return _cookies;
            }
        }

        public bool HasEntityBody => (_contentLength > 0 || _isChunked);

        public NameValueCollection Headers => _headers;

        public string HttpMethod => _method;

        public Stream InputStream
        {
            get
            {
                if (_inputStream == null)
                {
                    if (_isChunked || _contentLength > 0)
                        _inputStream = _context.Connection.GetRequestStream(_isChunked, _contentLength);
                    else
                        _inputStream = Stream.Null;
                }

                return _inputStream;
            }
        }

        public bool IsAuthenticated => false;

        public bool IsLocal => IPAddress.IsLoopback(RemoteEndPoint.Address) || LocalEndPoint.Address.Equals(RemoteEndPoint.Address);

        public bool IsSecureConnection => _context.Connection.IsSecure;

        public bool KeepAlive
        {
            get
            {
                if (_kaSet)
                    return _keepAlive;

                _kaSet = true;
                // 1. Connection header
                // 2. Protocol (1.1 == keep-alive by default)
                // 3. Keep-Alive header
                string cnc = _headers["Connection"];
                if (!String.IsNullOrEmpty(cnc))
                {
                    _keepAlive = (0 == String.Compare(cnc, "keep-alive", StringComparison.OrdinalIgnoreCase));
                }
                else if (_version == HttpVersion.Version11)
                {
                    _keepAlive = true;
                }
                else
                {
                    cnc = _headers["keep-alive"];
                    if (!String.IsNullOrEmpty(cnc))
                        _keepAlive = (0 != String.Compare(cnc, "closed", StringComparison.OrdinalIgnoreCase));
                }
                return _keepAlive;
            }
        }

        public IPEndPoint LocalEndPoint => _context.Connection.LocalEndPoint;

        public Version ProtocolVersion => _version;

        public NameValueCollection QueryString => _queryString;

        public string RawUrl => _rawUrl;

        public IPEndPoint RemoteEndPoint => _context.Connection.RemoteEndPoint;

        public Guid RequestTraceIdentifier => Guid.Empty;

        public Uri Url => _url;

        public Uri UrlReferrer => _referrer;

        public string UserAgent => _headers["user-agent"];

        public string UserHostAddress => LocalEndPoint.ToString();

        public string UserHostName => _headers["host"];

        public string[] UserLanguages => _userLanguages;

        public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state)
        {
            if (_gccDelegate == null)
                _gccDelegate = new GccDelegate(GetClientCertificate);
            return _gccDelegate.BeginInvoke(requestCallback, state);
        }

        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            if (_gccDelegate == null)
                throw new InvalidOperationException();

            return _gccDelegate.EndInvoke(asyncResult);
        }

        public X509Certificate2 GetClientCertificate()
        {
            return null;
            //return context.Connection.ClientCertificate;
        }

        public string ServiceName => null;

        private bool _websocketRequestWasSet;
        private bool _websocketRequest;

        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket connection request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket connection request; otherwise, <c>false</c>.
        /// </value>
        public bool IsWebSocketRequest
        {
            get
            {
                if (!_websocketRequestWasSet)
                {
                    _websocketRequest = _method == "GET" &&
                                        _version > HttpVersion.Version10 &&
                                        _headers.Contains("Upgrade", "websocket") &&
                                        _headers.Contains("Connection", "Upgrade");

                    _websocketRequestWasSet = true;
                }

                return _websocketRequest;
            }
        }

        public Task<X509Certificate2> GetClientCertificateAsync()
        {
            return Task<X509Certificate2>.Factory.FromAsync(BeginGetClientCertificate, EndGetClientCertificate, null);
        }
    }
}
