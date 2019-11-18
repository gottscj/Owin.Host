using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    internal sealed class HttpConnection
    {
        private static readonly AsyncCallback OnreadCb = new AsyncCallback(OnRead);
        private const int BufferSize = 8192;
        private Socket _sock;
        private readonly Stream _stream;
        private readonly EndPointListener _epl;
        private MemoryStream _ms;
        private byte[] _buffer;
        private HttpListenerContext _context;
        private StringBuilder _currentLine;
        private ListenerPrefix _prefix;
        private RequestStream _iStream;
        private ResponseStream _oStream;
        private bool _chunked;
        private int _reuses;
        private bool _contextBound;
        private readonly bool _secure;
        private readonly int _sTimeout = 300000; // 90k ms for first request, 15k ms from then on
        private readonly Timer _timer;
        private IPEndPoint _localEp;
        private HttpListener _lastListener;
        private X509Certificate _cert;
        private readonly SslStream _sslStream;

        private readonly ILogger _logger;

        public HttpConnection(ILogger logger, Socket sock, EndPointListener epl, bool secure, X509Certificate cert)
        {
            _logger = logger;
            _sock = sock;
            _epl = epl;
            _secure = secure;
            _cert = cert;
            if (secure == false)
            {
                _stream = new NetworkStream(sock, false);
            }
            else
            {
                //ssl_stream = epl.Listener.CreateSslStream(new NetworkStream(sock, false), false, (t, c, ch, e) =>
                //{
                //    if (c == null)
                //        return true;
                //    var c2 = c as X509Certificate2;
                //    if (c2 == null)
                //        c2 = new X509Certificate2(c.GetRawCertData());
                //    client_cert = c2;
                //    client_cert_errors = new int[] { (int)e };
                //    return true;
                //});
                //stream = ssl_stream.AuthenticatedStream;

                _sslStream = new SslStream(new NetworkStream(sock, false), false);
                _sslStream.AuthenticateAsServer(cert);
                _stream = _sslStream;
            }
            _timer = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
            Init();
        }

        public Stream Stream => _stream;

        private void Init()
        {
            if (_sslStream != null)
            {
                //ssl_stream.AuthenticateAsServer(client_cert, true, (SslProtocols)ServicePointManager.SecurityProtocol, false);
            }

            _contextBound = false;
            _iStream = null;
            _oStream = null;
            _prefix = null;
            _chunked = false;
            _ms = new MemoryStream();
            _position = 0;
            _inputState = InputState.RequestLine;
            _lineState = LineState.None;
            _context = new HttpListenerContext(this, _logger);
        }

        public bool IsClosed => (_sock == null);

        public int Reuses => _reuses;

        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (_localEp != null)
                    return _localEp;

                _localEp = (IPEndPoint)_sock.LocalEndPoint;
                return _localEp;
            }
        }

        public IPEndPoint RemoteEndPoint => (IPEndPoint)_sock.RemoteEndPoint;

        public bool IsSecure => _secure;

        public ListenerPrefix Prefix
        {
            get => _prefix;
            set => _prefix = value;
        }

        private void OnTimeout(object unused)
        {
            //_logger.Debug("HttpConnection keep alive timer fired. ConnectionId: {0}.", _connectionId);
            CloseSocket();
            Unbind();
        }

        public void BeginReadRequest()
        {
            if (_buffer == null)
                _buffer = new byte[BufferSize];
            try
            {
                //if (reuses == 1)
                //    s_timeout = 15000;
                _timer.Change(_sTimeout, Timeout.Infinite);
                _stream.BeginRead(_buffer, 0, BufferSize, OnreadCb, this);
            }
            catch
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                CloseSocket();
                Unbind();
            }
        }

        public RequestStream GetRequestStream(bool chunked, long contentlength)
        {
            if (_iStream == null)
            {
                byte[] buffer = _ms.GetBuffer();
                int length = (int)_ms.Length;
                _ms = null;
                if (chunked)
                {
                    _chunked = true;
                    //context.Response.SendChunked = true;
                    _iStream = new ChunkedInputStream(_context, _stream, buffer, _position, length - _position);
                }
                else {
                    _iStream = new RequestStream(_stream, buffer, _position, length - _position, contentlength);
                }
            }
            return _iStream;
        }

        public ResponseStream GetResponseStream()
        {
            // TODO: can we get this stream before reading the input?
            if (_oStream == null)
            {
                HttpListener listener = _context.Listener;

                if (listener == null)
                    return new ResponseStream(_stream, _context.Response, true);

                _oStream = new ResponseStream(_stream, _context.Response, listener.IgnoreWriteExceptions);
            }
            return _oStream;
        }

        private static void OnRead(IAsyncResult ares)
        {
            HttpConnection cnc = (HttpConnection)ares.AsyncState;
            cnc.OnReadInternal(ares);
        }

        private void OnReadInternal(IAsyncResult ares)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            int nread = -1;
            try
            {
                nread = _stream.EndRead(ares);
                _ms.Write(_buffer, 0, nread);
                if (_ms.Length > 32768)
                {
                    SendError("Bad request", 400);
                    Close(true);
                    return;
                }
            }
            catch (Exception ex)
            {
                OnReadInternalException(_ms, ex);
                return;
            }

            if (nread == 0)
            {
                //if (ms.Length > 0)
                //	SendError (); // Why bother?
                CloseSocket();
                Unbind();
                return;
            }

            if (ProcessInput(_ms))
            {
                if (!_context.HaveError)
                    _context.Request.FinishInitialization();

                if (_context.HaveError)
                {
                    SendError();
                    Close(true);
                    return;
                }

                if (!_epl.BindContext(_context))
                {
                    SendError("Invalid host", 400);
                    Close(true);
                    return;
                }
                HttpListener listener = _context.Listener;
                if (_lastListener != listener)
                {
                    RemoveConnection();
                    listener.AddConnection(this);
                    _lastListener = listener;
                }

                _contextBound = true;
                listener.RegisterContext(_context);
                return;
            }
            try
            {
                _stream.BeginRead(_buffer, 0, BufferSize, OnreadCb, this);
            }
            catch (IOException ex)
            {
                OnReadInternalException(_ms, ex);
            }
        }

        private void OnReadInternalException(MemoryStream ms, Exception ex)
        {
            //_logger.ErrorException("Error in HttpConnection.OnReadInternal", ex);

            if (ms != null && ms.Length > 0)
                SendError();
            if (_sock != null)
            {
                CloseSocket();
                Unbind();
            }
        }

        private void RemoveConnection()
        {
            if (_lastListener == null)
                _epl.RemoveConnection(this);
            else
                _lastListener.RemoveConnection(this);
        }

        private enum InputState
        {
            RequestLine,
            Headers
        }

        private enum LineState
        {
            None,
            Cr,
            Lf
        }

        private InputState _inputState = InputState.RequestLine;
        private LineState _lineState = LineState.None;
        private int _position;

        // true -> done processing
        // false -> need more input
        private bool ProcessInput(MemoryStream ms)
        {
            byte[] buffer = ms.GetBuffer();
            int len = (int)ms.Length;
            int used = 0;
            string line;

            while (true)
            {
                if (_context.HaveError)
                    return true;

                if (_position >= len)
                    break;

                try
                {
                    line = ReadLine(buffer, _position, len - _position, ref used);
                    _position += used;
                }
                catch
                {
                    _context.ErrorMessage = "Bad request";
                    _context.ErrorStatus = 400;
                    return true;
                }

                if (line == null)
                    break;

                if (line == "")
                {
                    if (_inputState == InputState.RequestLine)
                        continue;
                    _currentLine = null;
                    ms = null;
                    return true;
                }

                if (_inputState == InputState.RequestLine)
                {
                    _context.Request.SetRequestLine(line);
                    _inputState = InputState.Headers;
                }
                else
                {
                    try
                    {
                        _context.Request.AddHeader(line);
                    }
                    catch (Exception e)
                    {
                        _context.ErrorMessage = e.Message;
                        _context.ErrorStatus = 400;
                        return true;
                    }
                }
            }

            if (used == len)
            {
                ms.SetLength(0);
                _position = 0;
            }
            return false;
        }

        private string ReadLine(byte[] buffer, int offset, int len, ref int used)
        {
            if (_currentLine == null)
                _currentLine = new StringBuilder(128);
            int last = offset + len;
            used = 0;
            for (int i = offset; i < last && _lineState != LineState.Lf; i++)
            {
                used++;
                byte b = buffer[i];
                if (b == 13)
                {
                    _lineState = LineState.Cr;
                }
                else if (b == 10)
                {
                    _lineState = LineState.Lf;
                }
                else {
                    _currentLine.Append((char)b);
                }
            }

            string result = null;
            if (_lineState == LineState.Lf)
            {
                _lineState = LineState.None;
                result = _currentLine.ToString();
                _currentLine.Length = 0;
            }

            return result;
        }

        public void SendError(string msg, int status)
        {
            try
            {
                HttpListenerResponse response = _context.Response;
                response.StatusCode = status;
                response.ContentType = "text/html";
                string description = HttpListenerResponse.GetStatusDescription(status);
                string str;
                if (msg != null)
                    str = String.Format("<h1>{0} ({1})</h1>", description, msg);
                else
                    str = String.Format("<h1>{0}</h1>", description);

                byte[] error = _context.Response.ContentEncoding.GetBytes(str);
                response.Close(error, false);
            }
            catch
            {
                // response was already closed
            }
        }

        public void SendError()
        {
            SendError(_context.ErrorMessage, _context.ErrorStatus);
        }

        private void Unbind()
        {
            if (_contextBound)
            {
                _epl.UnbindContext(_context);
                _contextBound = false;
            }
        }

        public void Close()
        {
            Close(false);
        }

        private void CloseSocket()
        {
            if (_sock == null)
                return;

            try
            {
                _sock.Close();
            }
            catch
            {
            }
            finally
            {
                _sock = null;
            }
            RemoveConnection();
        }

        internal void Close(bool forceClose)
        {
            if (_sock != null)
            {
                if (!_context.Request.IsWebSocketRequest || forceClose)
                {
                    Stream st = GetResponseStream();
                    if (st != null)
                        st.Close();

                    _oStream = null;
                }
            }

            if (_sock != null)
            {
                forceClose |= !_context.Request.KeepAlive;
                if (!forceClose)
                    forceClose = (_context.Response.Headers["connection"] == "close");
                /*
				if (!force_close) {
//					bool conn_close = (status_code == 400 || status_code == 408 || status_code == 411 ||
//							status_code == 413 || status_code == 414 || status_code == 500 ||
//							status_code == 503);
					force_close |= (context.Request.ProtocolVersion <= HttpVersion.Version10);
				}
				*/

                if (!forceClose && _context.Request.FlushInput())
                {
                    if (_chunked && _context.Response.ForceCloseChunked == false)
                    {
                        // Don't close. Keep working.
                        _reuses++;
                        Unbind();
                        Init();
                        BeginReadRequest();
                        return;
                    }

                    _reuses++;
                    Unbind();
                    Init();
                    BeginReadRequest();
                    return;
                }

                Socket s = _sock;
                _sock = null;
                try
                {
                    if (s != null)
                        s.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
                finally
                {
                    if (s != null)
                        s.Close();
                }
                Unbind();
                RemoveConnection();
                return;
            }
        }
    }
}