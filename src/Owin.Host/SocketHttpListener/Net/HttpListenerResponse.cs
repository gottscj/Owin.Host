using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
#pragma warning disable 649

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    public sealed class HttpListenerResponse : IDisposable
    {
        private bool _disposed;
        private Encoding _contentEncoding;
        private long _contentLength;
        private bool _clSet;
        private string _contentType;
        private CookieCollection _cookies;
        private WebHeaderCollection _headers = new WebHeaderCollection();
        private bool _keepAlive = true;
        private ResponseStream _outputStream;
        private Version _version = HttpVersion.Version11;
        private string _location;
        private int _statusCode = 200;
        private string _statusDescription = "OK";
        private bool _chunked;
        private readonly HttpListenerContext _context;

        internal bool HeadersSent;
        internal object HeadersLock = new object();

        private bool _forceCloseChunked;

        private readonly ILogger _logger;

        internal HttpListenerResponse(HttpListenerContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        internal bool CloseConnection => _headers["Connection"] == "close";

        internal bool ForceCloseChunked => _forceCloseChunked;

        public Encoding ContentEncoding
        {
            get
            {
                if (_contentEncoding == null)
                    _contentEncoding = Encoding.Default;
                return _contentEncoding;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                _contentEncoding = value;
            }
        }

        public long ContentLength64
        {
            get => _contentLength;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (HeadersSent)
                    throw new InvalidOperationException("Cannot be changed after headers are sent.");

                if (value < 0)
                    throw new ArgumentOutOfRangeException("Must be >= 0", "value");

                _clSet = true;
                _contentLength = value;
            }
        }

        public string ContentType
        {
            get => _contentType;
            set
            {
                // TODO: is null ok?
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                _contentType = value;
            }
        }

        // RFC 2109, 2965 + the netscape specification at http://wp.netscape.com/newsref/std/cookie_spec.html
        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                    _cookies = new CookieCollection();
                return _cookies;
            }
            set => _cookies = value;
// null allowed?
        }

        public WebHeaderCollection Headers
        {
            get => _headers;
            set => _headers = value;
        }

        public bool KeepAlive
        {
            get => _keepAlive;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                _keepAlive = value;
            }
        }

        public Stream OutputStream
        {
            get
            {
                if (_outputStream == null)
                    _outputStream = _context.Connection.GetResponseStream();
                return _outputStream;
            }
        }

        public Version ProtocolVersion
        {
            get => _version;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
                    throw new ArgumentException("Must be 1.0 or 1.1", nameof(value));

                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                _version = value;
            }
        }

        public string RedirectLocation
        {
            get => _location;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                _location = value;
            }
        }

        public bool SendChunked
        {
            get => _chunked;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                _chunked = value;
            }
        }

        public int StatusCode
        {
            get => _statusCode;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (value < 100 || value > 999)
                    throw new ProtocolViolationException("StatusCode must be between 100 and 999.");
                _statusCode = value;
                _statusDescription = GetStatusDescription(value);
            }
        }

        internal static string GetStatusDescription(int code)
        {
            switch (code)
            {
                case 100: return "Continue";
                case 101: return "Switching Protocols";
                case 102: return "Processing";
                case 200: return "OK";
                case 201: return "Created";
                case 202: return "Accepted";
                case 203: return "Non-Authoritative Information";
                case 204: return "No Content";
                case 205: return "Reset Content";
                case 206: return "Partial Content";
                case 207: return "Multi-Status";
                case 300: return "Multiple Choices";
                case 301: return "Moved Permanently";
                case 302: return "Found";
                case 303: return "See Other";
                case 304: return "Not Modified";
                case 305: return "Use Proxy";
                case 307: return "Temporary Redirect";
                case 400: return "Bad Request";
                case 401: return "Unauthorized";
                case 402: return "Payment Required";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 406: return "Not Acceptable";
                case 407: return "Proxy Authentication Required";
                case 408: return "Request Timeout";
                case 409: return "Conflict";
                case 410: return "Gone";
                case 411: return "Length Required";
                case 412: return "Precondition Failed";
                case 413: return "Request Entity Too Large";
                case 414: return "Request-Uri Too Long";
                case 415: return "Unsupported Media Type";
                case 416: return "Requested Range Not Satisfiable";
                case 417: return "Expectation Failed";
                case 422: return "Unprocessable Entity";
                case 423: return "Locked";
                case 424: return "Failed Dependency";
                case 500: return "Internal Server Error";
                case 501: return "Not Implemented";
                case 502: return "Bad Gateway";
                case 503: return "Service Unavailable";
                case 504: return "Gateway Timeout";
                case 505: return "Http Version Not Supported";
                case 507: return "Insufficient Storage";
            }
            return "";
        }

        public string StatusDescription
        {
            get => _statusDescription;
            set => _statusDescription = value;
        }

        void IDisposable.Dispose()
        {
            Close(true); //TODO: Abort or Close?
        }

        public void Abort()
        {
            if (_disposed)
                return;

            Close(true);
        }

        public void AddHeader(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name == "")
                throw new ArgumentException("'name' cannot be empty", nameof(name));

            //TODO: check for forbidden headers and invalid characters
            if (value.Length > 65535)
                throw new ArgumentOutOfRangeException(nameof(value));

            _headers.Set(name, value);
        }

        public void AppendCookie(Cookie cookie)
        {
            if (cookie == null)
                throw new ArgumentNullException(nameof(cookie));

            Cookies.Add(cookie);
        }

        public void AppendHeader(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name == "")
                throw new ArgumentException("'name' cannot be empty", nameof(name));

            if (value.Length > 65535)
                throw new ArgumentOutOfRangeException(nameof(value));

            _headers.Add(name, value);
        }

        private void Close(bool force)
        {
            if (force)
            {
                _logger.Debug("HttpListenerResponse force closing HttpConnection");
            }
            _disposed = true;
            _context.Connection.Close(force);
        }

        public void Close()
        {
            if (_disposed)
                return;

            Close(false);
        }

        public void Close(byte[] responseEntity, bool willBlock)
        {
            if (_disposed)
                return;

            if (responseEntity == null)
                throw new ArgumentNullException(nameof(responseEntity));

            //TODO: if willBlock -> BeginWrite + Close ?
            ContentLength64 = responseEntity.Length;
            OutputStream.Write(responseEntity, 0, (int)_contentLength);
            Close(false);
        }

        public void CopyFrom(HttpListenerResponse templateResponse)
        {
            _headers.Clear();
            _headers.Add(templateResponse._headers);
            _contentLength = templateResponse._contentLength;
            _statusCode = templateResponse._statusCode;
            _statusDescription = templateResponse._statusDescription;
            _keepAlive = templateResponse._keepAlive;
            _version = templateResponse._version;
        }

        public void Redirect(string url)
        {
            StatusCode = 302; // Found
            _location = url;
        }

        private bool FindCookie(Cookie cookie)
        {
            string name = cookie.Name;
            string domain = cookie.Domain;
            string path = cookie.Path;
            foreach (Cookie c in _cookies)
            {
                if (name != c.Name)
                    continue;
                if (domain != c.Domain)
                    continue;
                if (path == c.Path)
                    return true;
            }

            return false;
        }

        internal void SendHeaders(bool closing, MemoryStream ms)
        {
            Encoding encoding = _contentEncoding;
            if (encoding == null)
                encoding = Encoding.Default;

            if (_contentType != null)
            {
                if (_contentEncoding != null && _contentType.IndexOf("charset=", StringComparison.Ordinal) == -1)
                {
                    string encName = _contentEncoding.WebName;
                    _headers.SetInternal("Content-Type", _contentType + "; charset=" + encName);
                }
                else
                {
                    _headers.SetInternal("Content-Type", _contentType);
                }
            }

            if (_headers["Server"] == null)
                _headers.SetInternal("Server", "Mono-HTTPAPI/1.0");

            CultureInfo inv = CultureInfo.InvariantCulture;
            if (_headers["Date"] == null)
                _headers.SetInternal("Date", DateTime.UtcNow.ToString("r", inv));

            if (!_chunked)
            {
                if (!_clSet && closing)
                {
                    _clSet = true;
                    _contentLength = 0;
                }

                if (_clSet)
                    _headers.SetInternal("Content-Length", _contentLength.ToString(inv));
            }

            Version v = _context.Request.ProtocolVersion;
            if (!_clSet && !_chunked && v >= HttpVersion.Version11)
                _chunked = true;

            /* Apache forces closing the connection for these status codes:
             *	HttpStatusCode.BadRequest 		400
             *	HttpStatusCode.RequestTimeout 		408
             *	HttpStatusCode.LengthRequired 		411
             *	HttpStatusCode.RequestEntityTooLarge 	413
             *	HttpStatusCode.RequestUriTooLong 	414
             *	HttpStatusCode.InternalServerError 	500
             *	HttpStatusCode.ServiceUnavailable 	503
             */
            bool connClose = (_statusCode == 400 || _statusCode == 408 || _statusCode == 411 ||
                    _statusCode == 413 || _statusCode == 414 || _statusCode == 500 ||
                    _statusCode == 503);

            if (connClose == false)
                connClose = !_context.Request.KeepAlive;

            // They sent both KeepAlive: true and Connection: close!?
            if (!_keepAlive || connClose)
            {
                _headers.SetInternal("Connection", "close");
                connClose = true;
            }

            if (_chunked)
                _headers.SetInternal("Transfer-Encoding", "chunked");

            //int reuses = context.Connection.Reuses;
            //if (reuses >= 100)
            //{
            //    _logger.Debug("HttpListenerResponse - keep alive has exceeded 100 uses and will be closed.");

            //    force_close_chunked = true;
            //    if (!conn_close)
            //    {
            //        headers.SetInternal("Connection", "close");
            //        conn_close = true;
            //    }
            //}

            if (!connClose)
            {
                if (_context.Request.ProtocolVersion <= HttpVersion.Version10)
                    _headers.SetInternal("Connection", "keep-alive");
            }

            if (_location != null)
                _headers.SetInternal("Location", _location);

            if (_cookies != null)
            {
                foreach (Cookie cookie in _cookies)
                    _headers.SetInternal("Set-Cookie", cookie.ToString());
            }

            using (StreamWriter writer = new StreamWriter(ms, encoding, 256, true))
            {
                writer.Write("HTTP/{0} {1} {2}\r\n", _version, _statusCode, _statusDescription);
                string headersStr = _headers.ToStringMultiValue();
                writer.Write(headersStr);
                writer.Flush();
            }

            int preamble = encoding.GetPreamble().Length;
            if (_outputStream == null)
                _outputStream = _context.Connection.GetResponseStream();

            /* Assumes that the ms was at position 0 */
            ms.Position = preamble;
            HeadersSent = true;
        }

        public void SetCookie(Cookie cookie)
        {
            if (cookie == null)
                throw new ArgumentNullException(nameof(cookie));

            if (_cookies != null)
            {
                if (FindCookie(cookie))
                    throw new ArgumentException("The cookie already exists.");
            }
            else
            {
                _cookies = new CookieCollection();
            }

            _cookies.Add(cookie);
        }
    }
}