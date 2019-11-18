using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Gottscj.Owin.Host.SocketHttpListener.Net.WebSockets;
using HttpStatusCode = Gottscj.Owin.Host.SocketHttpListener.Net.HttpStatusCode;
#pragma warning disable 169
#pragma warning disable 649

namespace Gottscj.Owin.Host.SocketHttpListener
{
    /// <summary>
    /// Implements the WebSocket interface.
    /// </summary>
    /// <remarks>
    /// The WebSocket class provides a set of methods and properties for two-way communication using
    /// the WebSocket protocol (<see href="http://tools.ietf.org/html/rfc6455">RFC 6455</see>).
    /// </remarks>
    public class WebSocket : IDisposable
    {
        #region Private Fields

        private string _base64Key;
        private Action _closeContext;
        private CompressionMethod _compression;
        private WebSocketContext _context;
        private CookieCollection _cookies;
        private string _extensions;
        private AutoResetEvent _exitReceiving;
        private object _forConn;
        private object _forEvent;
        private object _forMessageEventQueue;
        private object _forSend;
        private const string Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private Func<WebSocketContext, string>
                                        _handshakeRequestChecker;
        private Queue<MessageEventArgs> _messageEventQueue;
        private uint _nonceCount;
        private string _origin;
        private bool _preAuth;
        private string _protocol;
        private string[] _protocols;
        private Uri _proxyUri;
        private volatile WebSocketState _readyState;
        private AutoResetEvent _receivePong;
        private bool _secure;
        private Stream _stream;
        private Uri _uri;
        private const string Version = "13";

        #endregion

        #region Internal Fields

        internal const int FragmentLength = 1016; // Max value is int.MaxValue - 14.

        #endregion

        #region Internal Constructors

        // As server
        internal WebSocket(HttpListenerWebSocketContext context, string protocol)
        {
            _context = context;
            _protocol = protocol;

            _closeContext = context.Close;
            _secure = context.IsSecureConnection;
            _stream = context.Stream;

            Init();
        }

        #endregion

        // As server
        internal Func<WebSocketContext, string> CustomHandshakeRequestChecker
        {
            get
            {
                return _handshakeRequestChecker ?? (context => null);
            }

            set => _handshakeRequestChecker = value;
        }

        internal bool IsConnected => _readyState == WebSocketState.Open || _readyState == WebSocketState.Closing;

        /// <summary>
        /// Gets the state of the WebSocket connection.
        /// </summary>
        /// <value>
        /// One of the <see cref="WebSocketState"/> enum values, indicates the state of the WebSocket
        /// connection. The default value is <see cref="WebSocketState.Connecting"/>.
        /// </value>
        public WebSocketState ReadyState => _readyState;

        #region Public Events

        /// <summary>
        /// Occurs when the WebSocket connection has been closed.
        /// </summary>
        public event EventHandler<CloseEventArgs> OnClose;

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> gets an error.
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnError;

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> receives a message.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessage;

        /// <summary>
        /// Occurs when the WebSocket connection has been established.
        /// </summary>
        public event EventHandler OnOpen;

        #endregion

        #region Private Methods

        // As server
        private bool AcceptHandshake()
        {
            var msg = CheckIfValidHandshakeRequest(_context);
            if (msg != null)
            {
                Error("An error has occurred while connecting: " + msg);
                Close(HttpStatusCode.BadRequest);

                return false;
            }

            if (_protocol != null &&
                !_context.SecWebSocketProtocols.Contains(protocol => protocol == _protocol))
                _protocol = null;

            ////var extensions = _context.Headers["Sec-WebSocket-Extensions"];
            ////if (extensions != null && extensions.Length > 0)
            ////    processSecWebSocketExtensionsHeader(extensions);

            return SendHttpResponse(CreateHandshakeResponse());
        }

        // As server
        private string CheckIfValidHandshakeRequest(WebSocketContext context)
        {
            var headers = context.Headers;
            return context.RequestUri == null
                   ? "Invalid request url."
                   : !context.IsWebSocketRequest
                     ? "Not WebSocket connection request."
                     : !ValidateSecWebSocketKeyHeader(headers["Sec-WebSocket-Key"])
                       ? "Invalid Sec-WebSocket-Key header."
                       : !ValidateSecWebSocketVersionClientHeader(headers["Sec-WebSocket-Version"])
                         ? "Invalid Sec-WebSocket-Version header."
                         : CustomHandshakeRequestChecker(context);
        }

        private void Close(CloseStatusCode code, string reason, bool wait)
        {
            Close(new PayloadData(((ushort)code).Append(reason)), !code.IsReserved(), wait);
        }

        private void Close(PayloadData payload, bool send, bool wait)
        {
            lock (_forConn)
            {
                if (_readyState == WebSocketState.Closing || _readyState == WebSocketState.Closed)
                {
                    return;
                }

                _readyState = WebSocketState.Closing;
            }

            var e = new CloseEventArgs(payload);
            e.WasClean =
              CloseHandshake(
                  send ? WebSocketFrame.CreateCloseFrame(Mask.Unmask, payload).ToByteArray() : null,
                  wait ? 1000 : 0,
                  CloseServerResources);

            _readyState = WebSocketState.Closed;
            try
            {
                OnClose.Emit(this, e);
            }
            catch (Exception ex)
            {
                Error("An exception has occurred while OnClose.", ex);
            }
        }

        private bool CloseHandshake(byte[] frameAsBytes, int millisecondsTimeout, Action release)
        {
            var sent = frameAsBytes != null && WriteBytes(frameAsBytes);
            var received =
              millisecondsTimeout == 0 ||
              (sent && _exitReceiving != null && _exitReceiving.WaitOne(millisecondsTimeout));

            release();
            if (_receivePong != null)
            {
                _receivePong.Dispose();
                _receivePong = null;
            }

            if (_exitReceiving != null)
            {
                _exitReceiving.Dispose();
                _exitReceiving = null;
            }

            var result = sent && received;

            return result;
        }

        // As server
        private void CloseServerResources()
        {
            if (_closeContext == null)
                return;

            _closeContext();
            _closeContext = null;
            _stream = null;
            _context = null;
        }

        private bool ConcatenateFragmentsInto(Stream dest)
        {
            while (true)
            {
                var frame = WebSocketFrame.Read(_stream, true);
                if (frame.IsFinal)
                {
                    /* FINAL */

                    // CONT
                    if (frame.IsContinuation)
                    {
                        dest.WriteBytes(frame.PayloadData.ApplicationData);
                        break;
                    }

                    // PING
                    if (frame.IsPing)
                    {
                        ProcessPingFrame(frame);
                        continue;
                    }

                    // PONG
                    if (frame.IsPong)
                    {
                        ProcessPongFrame(frame);
                        continue;
                    }

                    // CLOSE
                    if (frame.IsClose)
                        return ProcessCloseFrame(frame);
                }
                else
                {
                    /* MORE */

                    // CONT
                    if (frame.IsContinuation)
                    {
                        dest.WriteBytes(frame.PayloadData.ApplicationData);
                        continue;
                    }
                }

                // ?
                return ProcessUnsupportedFrame(
                  frame,
                  CloseStatusCode.IncorrectData,
                  "An incorrect data has been received while receiving fragmented data.");
            }

            return true;
        }

        // As server
        private HttpResponse CreateHandshakeCloseResponse(HttpStatusCode code)
        {
            var res = HttpResponse.CreateCloseResponse(code);
            res.Headers["Sec-WebSocket-Version"] = Version;

            return res;
        }

        // As server
        private HttpResponse CreateHandshakeResponse()
        {
            var res = HttpResponse.CreateWebSocketResponse();

            var headers = res.Headers;
            headers["Sec-WebSocket-Accept"] = CreateResponseKey(_base64Key);

            if (_protocol != null)
                headers["Sec-WebSocket-Protocol"] = _protocol;

            if (_extensions != null)
                headers["Sec-WebSocket-Extensions"] = _extensions;

            if (_cookies.Count > 0)
                res.SetCookies(_cookies);

            return res;
        }

        private MessageEventArgs DequeueFromMessageEventQueue()
        {
            lock (_forMessageEventQueue)
                return _messageEventQueue.Count > 0
                       ? _messageEventQueue.Dequeue()
                       : null;
        }

        private void EnqueueToMessageEventQueue(MessageEventArgs e)
        {
            lock (_forMessageEventQueue)
                _messageEventQueue.Enqueue(e);
        }

        private void Error(string message, Exception exception)
        {
            try
            {
                if (exception != null)
                {
                    message += ". Exception.Message: " + exception.Message;
                }
                OnError.Emit(this, new ErrorEventArgs(message));
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Error(string message)
        {
            try
            {
                OnError.Emit(this, new ErrorEventArgs(message));
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Init()
        {
            _compression = CompressionMethod.None;
            _cookies = new CookieCollection();
            _forConn = new object();
            _forEvent = new object();
            _forSend = new object();
            _messageEventQueue = new Queue<MessageEventArgs>();
            _forMessageEventQueue = ((ICollection)_messageEventQueue).SyncRoot;
            _readyState = WebSocketState.Connecting;
        }

        private void Open()
        {
            try
            {
                StartReceiving();

                lock (_forEvent)
                {
                    try
                    {
                        OnOpen.Emit(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        ProcessException(ex, "An exception has occurred while OnOpen.");
                    }
                }
            }
            catch (Exception ex)
            {
                ProcessException(ex, "An exception has occurred while opening.");
            }
        }

        private bool ProcessCloseFrame(WebSocketFrame frame)
        {
            var payload = frame.PayloadData;
            Close(payload, !payload.ContainsReservedCloseStatusCode, false);

            return false;
        }

        private bool ProcessDataFrame(WebSocketFrame frame)
        {
            var e = frame.IsCompressed
                    ? new MessageEventArgs(
                        frame.Opcode, frame.PayloadData.ApplicationData.Decompress(_compression))
                    : new MessageEventArgs(frame.Opcode, frame.PayloadData);

            EnqueueToMessageEventQueue(e);
            return true;
        }

        private void ProcessException(Exception exception, string message)
        {
            var code = CloseStatusCode.Abnormal;
            var reason = message;
            if (exception is WebSocketException)
            {
                var wsex = (WebSocketException)exception;
                code = wsex.Code;
                reason = wsex.Message;
            }

            Error(message ?? code.GetMessage(), exception);
            if (_readyState == WebSocketState.Connecting)
                Close(HttpStatusCode.BadRequest);
            else
                Close(code, reason ?? code.GetMessage(), false);
        }

        private bool ProcessFragmentedFrame(WebSocketFrame frame)
        {
            return frame.IsContinuation // Not first fragment
                   ? true
                   : ProcessFragments(frame);
        }

        private bool ProcessFragments(WebSocketFrame first)
        {
            using (var buff = new MemoryStream())
            {
                buff.WriteBytes(first.PayloadData.ApplicationData);
                if (!ConcatenateFragmentsInto(buff))
                    return false;

                byte[] data;
                if (_compression != CompressionMethod.None)
                {
                    data = buff.DecompressToArray(_compression);
                }
                else
                {
                    data = buff.ToArray();
                }

                EnqueueToMessageEventQueue(new MessageEventArgs(first.Opcode, data));
                return true;
            }
        }

        private bool ProcessPingFrame(WebSocketFrame frame)
        {
            return true;
        }

        private bool ProcessPongFrame(WebSocketFrame frame)
        {
            _receivePong.Set();

            return true;
        }

        private bool ProcessUnsupportedFrame(WebSocketFrame frame, CloseStatusCode code, string reason)
        {
            ProcessException(new WebSocketException(code, reason), null);

            return false;
        }

        private bool ProcessWebSocketFrame(WebSocketFrame frame)
        {
            return frame.IsCompressed && _compression == CompressionMethod.None
                   ? ProcessUnsupportedFrame(
                       frame,
                       CloseStatusCode.IncorrectData,
                       "A compressed data has been received without available decompression method.")
                   : frame.IsFragmented
                     ? ProcessFragmentedFrame(frame)
                     : frame.IsData
                       ? ProcessDataFrame(frame)
                       : frame.IsPing
                         ? ProcessPingFrame(frame)
                         : frame.IsPong
                           ? ProcessPongFrame(frame)
                           : frame.IsClose
                             ? ProcessCloseFrame(frame)
                             : ProcessUnsupportedFrame(frame, CloseStatusCode.PolicyViolation, null);
        }

        private bool Send(Opcode opcode, Stream stream)
        {
            lock (_forSend)
            {
                var src = stream;
                var compressed = false;
                var sent = false;
                try
                {
                    if (_compression != CompressionMethod.None)
                    {
                        stream = stream.Compress(_compression);
                        compressed = true;
                    }

                    sent = Send(opcode, Mask.Unmask, stream, compressed);
                    if (!sent)
                        Error("Sending a data has been interrupted.");
                }
                catch (Exception ex)
                {
                    Error("An exception has occurred while sending a data.", ex);
                }
                finally
                {
                    if (compressed)
                        stream.Dispose();

                    src.Dispose();
                }

                return sent;
            }
        }

        private bool Send(Opcode opcode, Mask mask, Stream stream, bool compressed)
        {
            var len = stream.Length;

            /* Not fragmented */

            if (len == 0)
                return Send(Fin.Final, opcode, mask, new byte[0], compressed);

            var quo = len / FragmentLength;
            var rem = (int)(len % FragmentLength);

            byte[] buff = null;
            if (quo == 0)
            {
                buff = new byte[rem];
                return stream.Read(buff, 0, rem) == rem &&
                       Send(Fin.Final, opcode, mask, buff, compressed);
            }

            buff = new byte[FragmentLength];
            if (quo == 1 && rem == 0)
                return stream.Read(buff, 0, FragmentLength) == FragmentLength &&
                       Send(Fin.Final, opcode, mask, buff, compressed);

            /* Send fragmented */

            // Begin
            if (stream.Read(buff, 0, FragmentLength) != FragmentLength ||
                !Send(Fin.More, opcode, mask, buff, compressed))
                return false;

            var n = rem == 0 ? quo - 2 : quo - 1;
            for (long i = 0; i < n; i++)
                if (stream.Read(buff, 0, FragmentLength) != FragmentLength ||
                    !Send(Fin.More, Opcode.Cont, mask, buff, compressed))
                    return false;

            // End
            if (rem == 0)
                rem = FragmentLength;
            else
                buff = new byte[rem];

            return stream.Read(buff, 0, rem) == rem &&
                   Send(Fin.Final, Opcode.Cont, mask, buff, compressed);
        }

        private bool Send(Fin fin, Opcode opcode, Mask mask, byte[] data, bool compressed)
        {
            lock (_forConn)
            {
                if (_readyState != WebSocketState.Open)
                {
                    return false;
                }

                return WriteBytes(
                  WebSocketFrame.CreateWebSocketFrame(fin, opcode, mask, data, compressed).ToByteArray());
            }
        }

        private void SendAsync(Opcode opcode, Stream stream, Action<bool> completed)
        {
            Func<Opcode, Stream, bool> sender = Send;
            sender.BeginInvoke(
              opcode,
              stream,
              ar =>
              {
                  try
                  {
                      var sent = sender.EndInvoke(ar);
                      if (completed != null)
                          completed(sent);
                  }
                  catch (Exception ex)
                  {
                      Error("An exception has occurred while callback.", ex);
                  }
              },
              null);
        }

        // As server
        private bool SendHttpResponse(HttpResponse response)
        {
            return WriteBytes(response.ToByteArray());
        }

        private void StartReceiving()
        {
            lock (_forMessageEventQueue)
            {
                if (_messageEventQueue.Count > 0) _messageEventQueue.Clear();
            }
            _exitReceiving = new AutoResetEvent(false);
            _receivePong = new AutoResetEvent(false);

            Action receive = null;
            receive = () => WebSocketFrame.ReadAsync(
              _stream,
              true,
              frame =>
              {
                  if (ProcessWebSocketFrame(frame) && _readyState != WebSocketState.Closed)
                  {
                      receive();

                      if (!frame.IsData)
                          return;

                      lock (_forEvent)
                      {
                          try
                          {
                              var e = DequeueFromMessageEventQueue();
                              if (e != null && _readyState == WebSocketState.Open)
                                  OnMessage.Emit(this, e);
                          }
                          catch (Exception ex)
                          {
                              ProcessException(ex, "An exception has occurred while OnMessage.");
                          }
                      }
                  }
                  else
                  {
                      _exitReceiving?.Set();
                  }
              },
              ex => ProcessException(ex, "An exception has occurred while receiving a message."));

            receive();
        }

        // As server
        private bool ValidateSecWebSocketKeyHeader(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            _base64Key = value;
            return true;
        }

        // As server
        private bool ValidateSecWebSocketVersionClientHeader(string value)
        {
            return true;
            //return value != null && value == _version;
        }

        private bool WriteBytes(byte[] data)
        {
            try
            {
                _stream.Write(data, 0, data.Length);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Internal Methods

        // As server
        internal void Close(HttpResponse response)
        {
            _readyState = WebSocketState.Closing;

            SendHttpResponse(response);
            CloseServerResources();

            _readyState = WebSocketState.Closed;
        }

        // As server
        internal void Close(HttpStatusCode code)
        {
            Close(CreateHandshakeCloseResponse(code));
        }

        // As server
        public void ConnectAsServer()
        {
            try
            {
                if (AcceptHandshake())
                {
                    _readyState = WebSocketState.Open;
                    Open();
                }
            }
            catch (Exception ex)
            {
                ProcessException(ex, "An exception has occurred while connecting.");
            }
        }

        internal static string CreateResponseKey(string base64Key)
        {
            var buff = new StringBuilder(base64Key, 64);
            buff.Append(Guid);
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            var src = sha1.ComputeHash(Encoding.UTF8.GetBytes(buff.ToString()));

            return Convert.ToBase64String(src);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Closes the WebSocket connection, and releases all associated resources.
        /// </summary>
        public void Close()
        {
            var msg = _readyState.CheckIfClosable();
            if (msg != null)
            {
                Error(msg);

                return;
            }

            var send = _readyState == WebSocketState.Open;
            Close(new PayloadData(), send, send);
        }

        /// <summary>
        /// Closes the WebSocket connection with the specified <see cref="CloseStatusCode"/>
        /// and <see cref="string"/>, and releases all associated resources.
        /// </summary>
        /// <remarks>
        /// This method emits a <see cref="OnError"/> event if the size
        /// of <paramref name="reason"/> is greater than 123 bytes.
        /// </remarks>
        /// <param name="code">
        /// One of the <see cref="CloseStatusCode"/> enum values, represents the status code
        /// indicating the reason for the close.
        /// </param>
        /// <param name="reason">
        /// A <see cref="string"/> that represents the reason for the close.
        /// </param>
        public void Close(CloseStatusCode code, string reason)
        {
            byte[] data = null;
            var msg = _readyState.CheckIfClosable() ??
                      (data = ((ushort)code).Append(reason)).CheckIfValidControlData("reason");

            if (msg != null)
            {
                Error(msg);

                return;
            }

            var send = _readyState == WebSocketState.Open && !code.IsReserved();
            Close(new PayloadData(data), send, send);
        }

        /// <summary>
        /// Sends a binary <paramref name="data"/> asynchronously using the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method doesn't wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <param name="completed">
        /// An Action&lt;bool&gt; delegate that references the method(s) called when the send is
        /// complete. A <see cref="bool"/> passed to this delegate is <c>true</c> if the send is
        /// complete successfully; otherwise, <c>false</c>.
        /// </param>
        public void SendAsync(byte[] data, Action<bool> completed)
        {
            var msg = _readyState.CheckIfOpen() ?? data.CheckIfValidSendData();
            if (msg != null)
            {
                Error(msg);

                return;
            }

            SendAsync(Opcode.Binary, new MemoryStream(data), completed);
        }

        /// <summary>
        /// Sends a text <paramref name="data"/> asynchronously using the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method doesn't wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <param name="completed">
        /// An Action&lt;bool&gt; delegate that references the method(s) called when the send is
        /// complete. A <see cref="bool"/> passed to this delegate is <c>true</c> if the send is
        /// complete successfully; otherwise, <c>false</c>.
        /// </param>
        public void SendAsync(string data, Action<bool> completed)
        {
            var msg = _readyState.CheckIfOpen() ?? data.CheckIfValidSendData();
            if (msg != null)
            {
                Error(msg);

                return;
            }

            SendAsync(Opcode.Text, new MemoryStream(Encoding.UTF8.GetBytes(data)), completed);
        }

        #endregion

        #region Explicit Interface Implementation

        /// <summary>
        /// Closes the WebSocket connection, and releases all associated resources.
        /// </summary>
        /// <remarks>
        /// This method closes the WebSocket connection with <see cref="CloseStatusCode.Away"/>.
        /// </remarks>
        void IDisposable.Dispose()
        {
            Close(CloseStatusCode.Away, null);
        }

        #endregion
    }
}