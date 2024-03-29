using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Gottscj.Owin.Host.SocketHttpListener.Net;

namespace WebSocketSharp.Owin.RequestProcessing
{
	internal partial class CallEnvironment
    {
        // Mark all fields with delay initialization support as set.
        private UInt32 _flag0 = 0x5fc80210u;
        private UInt32 _flag1 = 0x0u;
        // Mark all fields with delay initialization support as requiring initialization.
        private UInt32 _initFlag0 = 0x5fc80210u;

        internal interface IPropertySource
        {
            Stream GetRequestBody();
            CancellationToken GetCallCancelled();
            IPrincipal GetServerUser();
            void SetServerUser(IPrincipal value);
            string GetServerRemoteIpAddress();
            string GetServerRemotePort();
            string GetServerLocalIpAddress();
            string GetServerLocalPort();
            bool GetServerIsLocal();
            bool TryGetClientCert(ref X509Certificate value);
            bool TryGetClientCertErrors(ref Exception value);
//            bool TryGetWebSocketAccept(ref WebSocketAccept value);
        }

        private string _RequestPath;
        private IDictionary<string, string[]> _ResponseHeaders;
        private IDictionary<string, string[]> _RequestHeaders;
        private Stream _ResponseBody;
        private Stream _RequestBody;
        private string _RequestId;
        private int _ResponseStatusCode;
        private string _ResponseReasonPhrase;
        private string _RequestQueryString;
        private CancellationToken _CallCancelled;
        private string _RequestMethod;
        private string _RequestScheme;
        private string _RequestPathBase;
        private string _RequestProtocol;
        private string _OwinVersion;
        private TextWriter _HostTraceOutput;
        private string _HostAppName;
        private string _HostAppMode;
        private CancellationToken _OnAppDisposing;
        private Action<Action<object>, object> _OnSendingHeaders;
        private IDictionary<string, object> _ServerCapabilities;
        private string _ServerRemoteIpAddress;
        private string _ServerRemotePort;
        private string _ServerLocalIpAddress;
        private string _ServerLocalPort;
        private bool _ServerIsLocal;
        private X509Certificate _ClientCert;
        private Exception _ClientCertErrors;
        private Func<Task> _LoadClientCert;
        private Func<string, long, long?, CancellationToken, Task> _SendFileAsync;
        private HttpListenerContext _RequestContext;

        private static readonly string HttpListenerContextName = typeof(HttpListenerContext).Name;
        private const int HttpListenerContextNameLength = 19;
        bool InitPropertyClientCert()
        {
            if (!_propertySource.TryGetClientCert(ref _ClientCert))
            {
                _flag0 &= ~0x8000000u;
                _initFlag0 &= ~0x8000000u;
                return false;
            }
            _initFlag0 &= ~0x8000000u;
            return true;
        }

        bool InitPropertyClientCertErrors()
        {
            if (!_propertySource.TryGetClientCertErrors(ref _ClientCertErrors))
            {
                _flag0 &= ~0x10000000u;
                _initFlag0 &= ~0x10000000u;
                return false;
            }
            _initFlag0 &= ~0x10000000u;
            return true;
        }

        bool InitPropertyWebSocketAccept()
        {
//            if (!_propertySource.TryGetWebSocketAccept(ref _WebSocketAccept))
            {
                _flag0 &= ~0x40000000u;
                _initFlag0 &= ~0x40000000u;
                return false;
            }
//            _initFlag0 &= ~0x40000000u;
//            return true;
        }

        internal bool ClientCertNeedsInit
        {
            get
            {
                return ((_initFlag0 & 0x8000000u) != 0);
            }
        }

        internal bool ClientCertErrorsNeedsInit
        {
            get
            {
                return ((_initFlag0 & 0x10000000u) != 0);
            }
        }

        internal bool WebSocketAcceptNeedsInit
        {
            get
            {
                return ((_initFlag0 & 0x40000000u) != 0);
            }
        }

        internal string RequestPath
        {
            get
            {
                return _RequestPath;
            }
            set
            {
                _flag0 |= 0x1u;
                _RequestPath = value;
            }
        }

        internal IDictionary<string, string[]> ResponseHeaders
        {
            get
            {
                return _ResponseHeaders;
            }
            set
            {
                _flag0 |= 0x2u;
                _ResponseHeaders = value;
            }
        }

        internal IDictionary<string, string[]> RequestHeaders
        {
            get
            {
                return _RequestHeaders;
            }
            set
            {
                _flag0 |= 0x4u;
                _RequestHeaders = value;
            }
        }

        internal Stream ResponseBody
        {
            get
            {
                return _ResponseBody;
            }
            set
            {
                _flag0 |= 0x8u;
                _ResponseBody = value;
            }
        }

        internal Stream RequestBody
        {
            get
            {
                if (((_initFlag0 & 0x10u) != 0))
                {
                    _RequestBody = _propertySource.GetRequestBody();
                    _initFlag0 &= ~0x10u;
                }
                return _RequestBody;
            }
            set
            {
                _initFlag0 &= ~0x10u;
                _flag0 |= 0x10u;
                _RequestBody = value;
            }
        }

        internal string RequestId
        {
            get
            {
                return _RequestId;
            }
            set
            {
                _flag0 |= 0x20u;
                _RequestId = value;
            }
        }

        internal int ResponseStatusCode
        {
            get
            {
                return _ResponseStatusCode;
            }
            set
            {
                _flag0 |= 0x40u;
                _ResponseStatusCode = value;
            }
        }

        internal string ResponseReasonPhrase
        {
            get
            {
                return _ResponseReasonPhrase;
            }
            set
            {
                _flag0 |= 0x80u;
                _ResponseReasonPhrase = value;
            }
        }

        internal string RequestQueryString
        {
            get
            {
                return _RequestQueryString;
            }
            set
            {
                _flag0 |= 0x100u;
                _RequestQueryString = value;
            }
        }

        internal CancellationToken CallCancelled
        {
            get
            {
                if (((_initFlag0 & 0x200u) != 0))
                {
                    _CallCancelled = _propertySource.GetCallCancelled();
                    _initFlag0 &= ~0x200u;
                }
                return _CallCancelled;
            }
            set
            {
                _initFlag0 &= ~0x200u;
                _flag0 |= 0x200u;
                _CallCancelled = value;
            }
        }

        internal string RequestMethod
        {
            get
            {
                return _RequestMethod;
            }
            set
            {
                _flag0 |= 0x400u;
                _RequestMethod = value;
            }
        }

        internal string RequestScheme
        {
            get
            {
                return _RequestScheme;
            }
            set
            {
                _flag0 |= 0x800u;
                _RequestScheme = value;
            }
        }

        internal string RequestPathBase
        {
            get
            {
                return _RequestPathBase;
            }
            set
            {
                _flag0 |= 0x1000u;
                _RequestPathBase = value;
            }
        }

        internal string RequestProtocol
        {
            get
            {
                return _RequestProtocol;
            }
            set
            {
                _flag0 |= 0x2000u;
                _RequestProtocol = value;
            }
        }

        internal string OwinVersion
        {
            get
            {
                return _OwinVersion;
            }
            set
            {
                _flag0 |= 0x4000u;
                _OwinVersion = value;
            }
        }

        internal TextWriter HostTraceOutput
        {
            get
            {
                return _HostTraceOutput;
            }
            set
            {
                _flag0 |= 0x8000u;
                _HostTraceOutput = value;
            }
        }

        internal string HostAppName
        {
            get
            {
                return _HostAppName;
            }
            set
            {
                _flag0 |= 0x10000u;
                _HostAppName = value;
            }
        }

        internal string HostAppMode
        {
            get
            {
                return _HostAppMode;
            }
            set
            {
                _flag0 |= 0x20000u;
                _HostAppMode = value;
            }
        }

        internal CancellationToken OnAppDisposing
        {
            get
            {
                return _OnAppDisposing;
            }
            set
            {
                _flag0 |= 0x40000u;
                _OnAppDisposing = value;
            }
        }

        internal IPrincipal ServerUser
        {
            get
            {
                return _propertySource.GetServerUser();
            }
            set
            {
                _propertySource.SetServerUser(value);
            }
        }

        internal Action<Action<object>, object> OnSendingHeaders
        {
            get
            {
                return _OnSendingHeaders;
            }
            set
            {
                _flag0 |= 0x100000u;
                _OnSendingHeaders = value;
            }
        }

        internal IDictionary<string, object> ServerCapabilities
        {
            get
            {
                return _ServerCapabilities;
            }
            set
            {
                _flag0 |= 0x200000u;
                _ServerCapabilities = value;
            }
        }

        internal string ServerRemoteIpAddress
        {
            get
            {
                if (((_initFlag0 & 0x400000u) != 0))
                {
                    _ServerRemoteIpAddress = _propertySource.GetServerRemoteIpAddress();
                    _initFlag0 &= ~0x400000u;
                }
                return _ServerRemoteIpAddress;
            }
            set
            {
                _initFlag0 &= ~0x400000u;
                _flag0 |= 0x400000u;
                _ServerRemoteIpAddress = value;
            }
        }

        internal string ServerRemotePort
        {
            get
            {
                if (((_initFlag0 & 0x800000u) != 0))
                {
                    _ServerRemotePort = _propertySource.GetServerRemotePort();
                    _initFlag0 &= ~0x800000u;
                }
                return _ServerRemotePort;
            }
            set
            {
                _initFlag0 &= ~0x800000u;
                _flag0 |= 0x800000u;
                _ServerRemotePort = value;
            }
        }

        internal string ServerLocalIpAddress
        {
            get
            {
                if (((_initFlag0 & 0x1000000u) != 0))
                {
                    _ServerLocalIpAddress = _propertySource.GetServerLocalIpAddress();
                    _initFlag0 &= ~0x1000000u;
                }
                return _ServerLocalIpAddress;
            }
            set
            {
                _initFlag0 &= ~0x1000000u;
                _flag0 |= 0x1000000u;
                _ServerLocalIpAddress = value;
            }
        }

        internal string ServerLocalPort
        {
            get
            {
                if (((_initFlag0 & 0x2000000u) != 0))
                {
                    _ServerLocalPort = _propertySource.GetServerLocalPort();
                    _initFlag0 &= ~0x2000000u;
                }
                return _ServerLocalPort;
            }
            set
            {
                _initFlag0 &= ~0x2000000u;
                _flag0 |= 0x2000000u;
                _ServerLocalPort = value;
            }
        }

        internal bool ServerIsLocal
        {
            get
            {
                if (((_initFlag0 & 0x4000000u) != 0))
                {
                    _ServerIsLocal = _propertySource.GetServerIsLocal();
                    _initFlag0 &= ~0x4000000u;
                }
                return _ServerIsLocal;
            }
            set
            {
                _initFlag0 &= ~0x4000000u;
                _flag0 |= 0x4000000u;
                _ServerIsLocal = value;
            }
        }

        internal X509Certificate ClientCert
        {
            get
            {
                if (((_initFlag0 & 0x8000000u) != 0))
                {
                    InitPropertyClientCert();
                }
                return _ClientCert;
            }
            set
            {
                _initFlag0 &= ~0x8000000u;
                _flag0 |= 0x8000000u;
                _ClientCert = value;
            }
        }

        internal Exception ClientCertErrors
        {
            get
            {
                if (((_initFlag0 & 0x10000000u) != 0))
                {
                    InitPropertyClientCertErrors();
                }
                return _ClientCertErrors;
            }
            set
            {
                _initFlag0 &= ~0x10000000u;
                _flag0 |= 0x10000000u;
                _ClientCertErrors = value;
            }
        }

        internal Func<Task> LoadClientCert
        {
            get
            {
                return _LoadClientCert;
            }
            set
            {
                _flag0 |= 0x20000000u;
                _LoadClientCert = value;
            }
        }

//        internal WebSocketAccept WebSocketAccept
//        {
//            get
//            {
//                if (((_initFlag0 & 0x40000000u) != 0))
//                {
//                    InitPropertyWebSocketAccept();
//                }
//                return _WebSocketAccept;
//            }
//            set
//            {
//                _initFlag0 &= ~0x40000000u;
//                _flag0 |= 0x40000000u;
//                _WebSocketAccept = value;
//            }
//        }

        internal Func<string, long, long?, CancellationToken, Task> SendFileAsync
        {
            get
            {
                return _SendFileAsync;
            }
            set
            {
                _flag0 |= 0x80000000u;
                _SendFileAsync = value;
            }
        }

        internal HttpListenerContext RequestContext
        {
            get
            {
                return _RequestContext;
            }
            set
            {
                _flag1 |= 0x1u;
                _RequestContext = value;
            }
        }

        private bool PropertiesContainsKey(string key)
        {
            switch (key.Length)
            {
                case 11:
                    if (((_flag0 & 0x80000u) != 0) && string.Equals(key, "server.User", StringComparison.Ordinal))
                    {
                        return true;
                    }
                   break;
                case 12:
                    if (((_flag0 & 0x4000u) != 0) && string.Equals(key, "owin.Version", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x10000u) != 0) && string.Equals(key, "host.AppName", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x20000u) != 0) && string.Equals(key, "host.AppMode", StringComparison.Ordinal))
                    {
                        return true;
                    }
                   break;
                case 14:
                    if (((_flag0 & 0x20u) != 0) && string.Equals(key, "owin.RequestId", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x4000000u) != 0) && string.Equals(key, "server.IsLocal", StringComparison.Ordinal))
                    {
                        return true;
                    }
                   break;
                case 16:
                    if (((_flag0 & 0x1u) != 0) && string.Equals(key, "owin.RequestPath", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x10u) != 0) && string.Equals(key, "owin.RequestBody", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x8000u) != 0) && string.Equals(key, "host.TraceOutput", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x2000000u) != 0) && string.Equals(key, "server.LocalPort", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x40000000u) != 0) && string.Equals(key, "websocket.Accept", StringComparison.Ordinal))
                    {
                        if (((_initFlag0 & 0x40000000u) == 0) || InitPropertyWebSocketAccept())
                        {
                            return true;
                        }
                    }
                   break;
                case 17:
                    if (((_flag0 & 0x8u) != 0) && string.Equals(key, "owin.ResponseBody", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x800000u) != 0) && string.Equals(key, "server.RemotePort", StringComparison.Ordinal))
                    {
                        return true;
                    }
                   break;
                case 18:
                    if (((_flag0 & 0x200u) != 0) && string.Equals(key, "owin.CallCancelled", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x400u) != 0) && string.Equals(key, "owin.RequestMethod", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x800u) != 0) && string.Equals(key, "owin.RequestScheme", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x80000000u) != 0) && string.Equals(key, "sendfile.SendAsync", StringComparison.Ordinal))
                    {
                        return true;
                    }
                   break;
                case 19:
                    if (((_flag0 & 0x4u) != 0) && string.Equals(key, "owin.RequestHeaders", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x40000u) != 0) && string.Equals(key, "host.OnAppDisposing", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x200000u) != 0) && string.Equals(key, "server.Capabilities", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag1 & 0x1u) != 0) && string.Equals(key, HttpListenerContextName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                   break;
                case 20:
                    if (((_flag0 & 0x2u) != 0) && string.Equals(key, "owin.ResponseHeaders", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x1000u) != 0) && string.Equals(key, "owin.RequestPathBase", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x2000u) != 0) && string.Equals(key, "owin.RequestProtocol", StringComparison.Ordinal))
                    {
                        return true;
                    }
                   break;
                case 21:
                    if (((_flag0 & 0x1000000u) != 0) && string.Equals(key, "server.LocalIpAddress", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x8000000u) != 0) && string.Equals(key, "ssl.ClientCertificate", StringComparison.Ordinal))
                    {
                        if (((_initFlag0 & 0x8000000u) == 0) || InitPropertyClientCert())
                        {
                            return true;
                        }
                    }
                   break;
                case 22:
                    if (((_flag0 & 0x400000u) != 0) && string.Equals(key, "server.RemoteIpAddress", StringComparison.Ordinal))
                    {
                        return true;
                    }
                   break;
                case 23:
                    if (((_flag0 & 0x40u) != 0) && string.Equals(key, "owin.ResponseStatusCode", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x100u) != 0) && string.Equals(key, "owin.RequestQueryString", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x100000u) != 0) && string.Equals(key, "server.OnSendingHeaders", StringComparison.Ordinal))
                    {
                        return true;
                    }
                    if (((_flag0 & 0x20000000u) != 0) && string.Equals(key, "ssl.LoadClientCertAsync", StringComparison.Ordinal))
                    {
                        return true;
                    }
                   break;
                case 25:
                    if (((_flag0 & 0x80u) != 0) && string.Equals(key, "owin.ResponseReasonPhrase", StringComparison.Ordinal))
                    {
                        return true;
                    }
                   break;
                case 27:
                    if (((_flag0 & 0x10000000u) != 0) && string.Equals(key, "ssl.ClientCertificateErrors", StringComparison.Ordinal))
                    {
                        if (((_initFlag0 & 0x10000000u) == 0) || InitPropertyClientCertErrors())
                        {
                            return true;
                        }
                    }
                   break;
            }
            return false;
        }

        private bool PropertiesTryGetValue(string key, out object value)
        {
            switch (key.Length)
            {
                case 11:
                    if (((_flag0 & 0x80000u) != 0) && string.Equals(key, "server.User", StringComparison.Ordinal))
                    {
                        value = ServerUser;
                        return true;
                    }
                   break;
                case 12:
                    if (((_flag0 & 0x4000u) != 0) && string.Equals(key, "owin.Version", StringComparison.Ordinal))
                    {
                        value = OwinVersion;
                        return true;
                    }
                    if (((_flag0 & 0x10000u) != 0) && string.Equals(key, "host.AppName", StringComparison.Ordinal))
                    {
                        value = HostAppName;
                        return true;
                    }
                    if (((_flag0 & 0x20000u) != 0) && string.Equals(key, "host.AppMode", StringComparison.Ordinal))
                    {
                        value = HostAppMode;
                        return true;
                    }
                   break;
                case 14:
                    if (((_flag0 & 0x20u) != 0) && string.Equals(key, "owin.RequestId", StringComparison.Ordinal))
                    {
                        value = RequestId;
                        return true;
                    }
                    if (((_flag0 & 0x4000000u) != 0) && string.Equals(key, "server.IsLocal", StringComparison.Ordinal))
                    {
                        value = ServerIsLocal;
                        return true;
                    }
                   break;
                case 16:
                    if (((_flag0 & 0x1u) != 0) && string.Equals(key, "owin.RequestPath", StringComparison.Ordinal))
                    {
                        value = RequestPath;
                        return true;
                    }
                    if (((_flag0 & 0x10u) != 0) && string.Equals(key, "owin.RequestBody", StringComparison.Ordinal))
                    {
                        value = RequestBody;
                        return true;
                    }
                    if (((_flag0 & 0x8000u) != 0) && string.Equals(key, "host.TraceOutput", StringComparison.Ordinal))
                    {
                        value = HostTraceOutput;
                        return true;
                    }
                    if (((_flag0 & 0x2000000u) != 0) && string.Equals(key, "server.LocalPort", StringComparison.Ordinal))
                    {
                        value = ServerLocalPort;
                        return true;
                    }
//                    if (((_flag0 & 0x40000000u) != 0) && string.Equals(key, "websocket.Accept", StringComparison.Ordinal))
//                    {
//                        value = WebSocketAccept;
//                        // Delayed initialization in the property getter may determine that the element is not actually present
//                        if (!((_flag0 & 0x40000000u) != 0))
//                        {
//                            value = default(WebSocketAccept);
//                            return false;
//                        }
//                        return true;
//                    }
                   break;
                case 17:
                    if (((_flag0 & 0x8u) != 0) && string.Equals(key, "owin.ResponseBody", StringComparison.Ordinal))
                    {
                        value = ResponseBody;
                        return true;
                    }
                    if (((_flag0 & 0x800000u) != 0) && string.Equals(key, "server.RemotePort", StringComparison.Ordinal))
                    {
                        value = ServerRemotePort;
                        return true;
                    }
                   break;
                case 18:
                    if (((_flag0 & 0x200u) != 0) && string.Equals(key, "owin.CallCancelled", StringComparison.Ordinal))
                    {
                        value = CallCancelled;
                        return true;
                    }
                    if (((_flag0 & 0x400u) != 0) && string.Equals(key, "owin.RequestMethod", StringComparison.Ordinal))
                    {
                        value = RequestMethod;
                        return true;
                    }
                    if (((_flag0 & 0x800u) != 0) && string.Equals(key, "owin.RequestScheme", StringComparison.Ordinal))
                    {
                        value = RequestScheme;
                        return true;
                    }
                    if (((_flag0 & 0x80000000u) != 0) && string.Equals(key, "sendfile.SendAsync", StringComparison.Ordinal))
                    {
                        value = SendFileAsync;
                        return true;
                    }
                   break;
                case 19:
                    if (((_flag0 & 0x4u) != 0) && string.Equals(key, "owin.RequestHeaders", StringComparison.Ordinal))
                    {
                        value = RequestHeaders;
                        return true;
                    }
                    if (((_flag0 & 0x40000u) != 0) && string.Equals(key, "host.OnAppDisposing", StringComparison.Ordinal))
                    {
                        value = OnAppDisposing;
                        return true;
                    }
                    if (((_flag0 & 0x200000u) != 0) && string.Equals(key, "server.Capabilities", StringComparison.Ordinal))
                    {
                        value = ServerCapabilities;
                        return true;
                    }
                    if (((_flag1 & 0x1u) != 0) && string.Equals(key, HttpListenerContextName, StringComparison.Ordinal))
                    {
                        value = RequestContext;
                        return true;
                    }
                   break;
                case 20:
                    if (((_flag0 & 0x2u) != 0) && string.Equals(key, "owin.ResponseHeaders", StringComparison.Ordinal))
                    {
                        value = ResponseHeaders;
                        return true;
                    }
                    if (((_flag0 & 0x1000u) != 0) && string.Equals(key, "owin.RequestPathBase", StringComparison.Ordinal))
                    {
                        value = RequestPathBase;
                        return true;
                    }
                    if (((_flag0 & 0x2000u) != 0) && string.Equals(key, "owin.RequestProtocol", StringComparison.Ordinal))
                    {
                        value = RequestProtocol;
                        return true;
                    }
                   break;
                case 21:
                    if (((_flag0 & 0x1000000u) != 0) && string.Equals(key, "server.LocalIpAddress", StringComparison.Ordinal))
                    {
                        value = ServerLocalIpAddress;
                        return true;
                    }
                    if (((_flag0 & 0x8000000u) != 0) && string.Equals(key, "ssl.ClientCertificate", StringComparison.Ordinal))
                    {
                        value = ClientCert;
                        // Delayed initialization in the property getter may determine that the element is not actually present
                        if (!((_flag0 & 0x8000000u) != 0))
                        {
                            value = default(X509Certificate);
                            return false;
                        }
                        return true;
                    }
                   break;
                case 22:
                    if (((_flag0 & 0x400000u) != 0) && string.Equals(key, "server.RemoteIpAddress", StringComparison.Ordinal))
                    {
                        value = ServerRemoteIpAddress;
                        return true;
                    }
                   break;
                case 23:
                    if (((_flag0 & 0x40u) != 0) && string.Equals(key, "owin.ResponseStatusCode", StringComparison.Ordinal))
                    {
                        value = ResponseStatusCode;
                        return true;
                    }
                    if (((_flag0 & 0x100u) != 0) && string.Equals(key, "owin.RequestQueryString", StringComparison.Ordinal))
                    {
                        value = RequestQueryString;
                        return true;
                    }
                    if (((_flag0 & 0x100000u) != 0) && string.Equals(key, "server.OnSendingHeaders", StringComparison.Ordinal))
                    {
                        value = OnSendingHeaders;
                        return true;
                    }
                    if (((_flag0 & 0x20000000u) != 0) && string.Equals(key, "ssl.LoadClientCertAsync", StringComparison.Ordinal))
                    {
                        value = LoadClientCert;
                        return true;
                    }
                   break;
                case 25:
                    if (((_flag0 & 0x80u) != 0) && string.Equals(key, "owin.ResponseReasonPhrase", StringComparison.Ordinal))
                    {
                        value = ResponseReasonPhrase;
                        return true;
                    }
                   break;
                case 27:
                    if (((_flag0 & 0x10000000u) != 0) && string.Equals(key, "ssl.ClientCertificateErrors", StringComparison.Ordinal))
                    {
                        value = ClientCertErrors;
                        // Delayed initialization in the property getter may determine that the element is not actually present
                        if (!((_flag0 & 0x10000000u) != 0))
                        {
                            value = default(Exception);
                            return false;
                        }
                        return true;
                    }
                   break;
            }
            value = null;
            return false;
        }

        private bool PropertiesTrySetValue(string key, object value)
        {
            switch (key.Length)
            {
                case 11:
                    if (string.Equals(key, "server.User", StringComparison.Ordinal))
                    {
                        ServerUser = (IPrincipal)value;
                        return true;
                    }
                   break;
                case 12:
                    if (string.Equals(key, "owin.Version", StringComparison.Ordinal))
                    {
                        OwinVersion = (string)value;
                        return true;
                    }
                    if (string.Equals(key, "host.AppName", StringComparison.Ordinal))
                    {
                        HostAppName = (string)value;
                        return true;
                    }
                    if (string.Equals(key, "host.AppMode", StringComparison.Ordinal))
                    {
                        HostAppMode = (string)value;
                        return true;
                    }
                   break;
                case 14:
                    if (string.Equals(key, "owin.RequestId", StringComparison.Ordinal))
                    {
                        RequestId = (string)value;
                        return true;
                    }
                    if (string.Equals(key, "server.IsLocal", StringComparison.Ordinal))
                    {
                        ServerIsLocal = (bool)value;
                        return true;
                    }
                   break;
                case 16:
                    if (string.Equals(key, "owin.RequestPath", StringComparison.Ordinal))
                    {
                        RequestPath = (string)value;
                        return true;
                    }
                    if (string.Equals(key, "owin.RequestBody", StringComparison.Ordinal))
                    {
                        RequestBody = (Stream)value;
                        return true;
                    }
                    if (string.Equals(key, "host.TraceOutput", StringComparison.Ordinal))
                    {
                        HostTraceOutput = (TextWriter)value;
                        return true;
                    }
                    if (string.Equals(key, "server.LocalPort", StringComparison.Ordinal))
                    {
                        ServerLocalPort = (string)value;
                        return true;
                    }
//                    if (string.Equals(key, "websocket.Accept", StringComparison.Ordinal))
//                    {
//                        WebSocketAccept = (WebSocketAccept)value;
//                        return true;
//                    }
                   break;
                case 17:
                    if (string.Equals(key, "owin.ResponseBody", StringComparison.Ordinal))
                    {
                        ResponseBody = (Stream)value;
                        return true;
                    }
                    if (string.Equals(key, "server.RemotePort", StringComparison.Ordinal))
                    {
                        ServerRemotePort = (string)value;
                        return true;
                    }
                   break;
                case 18:
                    if (string.Equals(key, "owin.CallCancelled", StringComparison.Ordinal))
                    {
                        CallCancelled = (CancellationToken)value;
                        return true;
                    }
                    if (string.Equals(key, "owin.RequestMethod", StringComparison.Ordinal))
                    {
                        RequestMethod = (string)value;
                        return true;
                    }
                    if (string.Equals(key, "owin.RequestScheme", StringComparison.Ordinal))
                    {
                        RequestScheme = (string)value;
                        return true;
                    }
                    if (string.Equals(key, "sendfile.SendAsync", StringComparison.Ordinal))
                    {
                        SendFileAsync = (Func<string, long, long?, CancellationToken, Task>)value;
                        return true;
                    }
                   break;
                case 19:
                    if (string.Equals(key, "owin.RequestHeaders", StringComparison.Ordinal))
                    {
                        RequestHeaders = (IDictionary<string, string[]>)value;
                        return true;
                    }
                    if (string.Equals(key, "host.OnAppDisposing", StringComparison.Ordinal))
                    {
                        OnAppDisposing = (CancellationToken)value;
                        return true;
                    }
                    if (string.Equals(key, "server.Capabilities", StringComparison.Ordinal))
                    {
                        ServerCapabilities = (IDictionary<string, object>)value;
                        return true;
                    }
                    if (string.Equals(key, HttpListenerContextName, StringComparison.Ordinal))
                    {
                        RequestContext = (HttpListenerContext)value;
                        return true;
                    }
                   break;
                case 20:
                    if (string.Equals(key, "owin.ResponseHeaders", StringComparison.Ordinal))
                    {
                        ResponseHeaders = (IDictionary<string, string[]>)value;
                        return true;
                    }
                    if (string.Equals(key, "owin.RequestPathBase", StringComparison.Ordinal))
                    {
                        RequestPathBase = (string)value;
                        return true;
                    }
                    if (string.Equals(key, "owin.RequestProtocol", StringComparison.Ordinal))
                    {
                        RequestProtocol = (string)value;
                        return true;
                    }
                   break;
                case 21:
                    if (string.Equals(key, "server.LocalIpAddress", StringComparison.Ordinal))
                    {
                        ServerLocalIpAddress = (string)value;
                        return true;
                    }
                    if (string.Equals(key, "ssl.ClientCertificate", StringComparison.Ordinal))
                    {
                        ClientCert = (X509Certificate)value;
                        return true;
                    }
                   break;
                case 22:
                    if (string.Equals(key, "server.RemoteIpAddress", StringComparison.Ordinal))
                    {
                        ServerRemoteIpAddress = (string)value;
                        return true;
                    }
                   break;
                case 23:
                    if (string.Equals(key, "owin.ResponseStatusCode", StringComparison.Ordinal))
                    {
                        ResponseStatusCode = (int)value;
                        return true;
                    }
                    if (string.Equals(key, "owin.RequestQueryString", StringComparison.Ordinal))
                    {
                        RequestQueryString = (string)value;
                        return true;
                    }
                    if (string.Equals(key, "server.OnSendingHeaders", StringComparison.Ordinal))
                    {
                        OnSendingHeaders = (Action<Action<object>, object>)value;
                        return true;
                    }
                    if (string.Equals(key, "ssl.LoadClientCertAsync", StringComparison.Ordinal))
                    {
                        LoadClientCert = (Func<Task>)value;
                        return true;
                    }
                   break;
                case 25:
                    if (string.Equals(key, "owin.ResponseReasonPhrase", StringComparison.Ordinal))
                    {
                        ResponseReasonPhrase = (string)value;
                        return true;
                    }
                   break;
                case 27:
                    if (string.Equals(key, "ssl.ClientCertificateErrors", StringComparison.Ordinal))
                    {
                        ClientCertErrors = (Exception)value;
                        return true;
                    }
                   break;
            }
            return false;
        }

        private bool PropertiesTryRemove(string key)
        {
            switch (key.Length)
            {
                case 11:
                    if (((_flag0 & 0x80000u) != 0) && string.Equals(key, "server.User", StringComparison.Ordinal))
                    {
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 12:
                    if (((_flag0 & 0x4000u) != 0) && string.Equals(key, "owin.Version", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x4000u;
                        _OwinVersion = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x10000u) != 0) && string.Equals(key, "host.AppName", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x10000u;
                        _HostAppName = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x20000u) != 0) && string.Equals(key, "host.AppMode", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x20000u;
                        _HostAppMode = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 14:
                    if (((_flag0 & 0x20u) != 0) && string.Equals(key, "owin.RequestId", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x20u;
                        _RequestId = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x4000000u) != 0) && string.Equals(key, "server.IsLocal", StringComparison.Ordinal))
                    {
                        _initFlag0 &= ~0x4000000u;
                        _flag0 &= ~0x4000000u;
                        _ServerIsLocal = default(bool);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 16:
                    if (((_flag0 & 0x1u) != 0) && string.Equals(key, "owin.RequestPath", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x1u;
                        _RequestPath = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x10u) != 0) && string.Equals(key, "owin.RequestBody", StringComparison.Ordinal))
                    {
                        _initFlag0 &= ~0x10u;
                        _flag0 &= ~0x10u;
                        _RequestBody = default(Stream);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x8000u) != 0) && string.Equals(key, "host.TraceOutput", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x8000u;
                        _HostTraceOutput = default(TextWriter);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x2000000u) != 0) && string.Equals(key, "server.LocalPort", StringComparison.Ordinal))
                    {
                        _initFlag0 &= ~0x2000000u;
                        _flag0 &= ~0x2000000u;
                        _ServerLocalPort = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
//                    if (((_flag0 & 0x40000000u) != 0) && string.Equals(key, "websocket.Accept", StringComparison.Ordinal))
//                    {
//                        _initFlag0 &= ~0x40000000u;
//                        _flag0 &= ~0x40000000u;
//                        _WebSocketAccept = default(WebSocketAccept);
//                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
//                        return true;
//                    }
                   break;
                case 17:
                    if (((_flag0 & 0x8u) != 0) && string.Equals(key, "owin.ResponseBody", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x8u;
                        _ResponseBody = default(Stream);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x800000u) != 0) && string.Equals(key, "server.RemotePort", StringComparison.Ordinal))
                    {
                        _initFlag0 &= ~0x800000u;
                        _flag0 &= ~0x800000u;
                        _ServerRemotePort = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 18:
                    if (((_flag0 & 0x200u) != 0) && string.Equals(key, "owin.CallCancelled", StringComparison.Ordinal))
                    {
                        _initFlag0 &= ~0x200u;
                        _flag0 &= ~0x200u;
                        _CallCancelled = default(CancellationToken);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x400u) != 0) && string.Equals(key, "owin.RequestMethod", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x400u;
                        _RequestMethod = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x800u) != 0) && string.Equals(key, "owin.RequestScheme", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x800u;
                        _RequestScheme = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x80000000u) != 0) && string.Equals(key, "sendfile.SendAsync", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x80000000u;
                        _SendFileAsync = default(Func<string, long, long?, CancellationToken, Task>);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 19:
                    if (((_flag0 & 0x4u) != 0) && string.Equals(key, "owin.RequestHeaders", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x4u;
                        _RequestHeaders = default(IDictionary<string, string[]>);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x40000u) != 0) && string.Equals(key, "host.OnAppDisposing", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x40000u;
                        _OnAppDisposing = default(CancellationToken);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x200000u) != 0) && string.Equals(key, "server.Capabilities", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x200000u;
                        _ServerCapabilities = default(IDictionary<string, object>);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag1 & 0x1u) != 0) && string.Equals(key, HttpListenerContextName, StringComparison.Ordinal))
                    {
                        _flag1 &= ~0x1u;
                        _RequestContext = default(HttpListenerContext);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 20:
                    if (((_flag0 & 0x2u) != 0) && string.Equals(key, "owin.ResponseHeaders", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x2u;
                        _ResponseHeaders = default(IDictionary<string, string[]>);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x1000u) != 0) && string.Equals(key, "owin.RequestPathBase", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x1000u;
                        _RequestPathBase = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x2000u) != 0) && string.Equals(key, "owin.RequestProtocol", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x2000u;
                        _RequestProtocol = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 21:
                    if (((_flag0 & 0x1000000u) != 0) && string.Equals(key, "server.LocalIpAddress", StringComparison.Ordinal))
                    {
                        _initFlag0 &= ~0x1000000u;
                        _flag0 &= ~0x1000000u;
                        _ServerLocalIpAddress = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x8000000u) != 0) && string.Equals(key, "ssl.ClientCertificate", StringComparison.Ordinal))
                    {
                        _initFlag0 &= ~0x8000000u;
                        _flag0 &= ~0x8000000u;
                        _ClientCert = default(X509Certificate);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 22:
                    if (((_flag0 & 0x400000u) != 0) && string.Equals(key, "server.RemoteIpAddress", StringComparison.Ordinal))
                    {
                        _initFlag0 &= ~0x400000u;
                        _flag0 &= ~0x400000u;
                        _ServerRemoteIpAddress = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 23:
                    if (((_flag0 & 0x40u) != 0) && string.Equals(key, "owin.ResponseStatusCode", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x40u;
                        _ResponseStatusCode = default(int);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x100u) != 0) && string.Equals(key, "owin.RequestQueryString", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x100u;
                        _RequestQueryString = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x100000u) != 0) && string.Equals(key, "server.OnSendingHeaders", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x100000u;
                        _OnSendingHeaders = default(Action<Action<object>, object>);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                    if (((_flag0 & 0x20000000u) != 0) && string.Equals(key, "ssl.LoadClientCertAsync", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x20000000u;
                        _LoadClientCert = default(Func<Task>);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 25:
                    if (((_flag0 & 0x80u) != 0) && string.Equals(key, "owin.ResponseReasonPhrase", StringComparison.Ordinal))
                    {
                        _flag0 &= ~0x80u;
                        _ResponseReasonPhrase = default(string);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
                case 27:
                    if (((_flag0 & 0x10000000u) != 0) && string.Equals(key, "ssl.ClientCertificateErrors", StringComparison.Ordinal))
                    {
                        _initFlag0 &= ~0x10000000u;
                        _flag0 &= ~0x10000000u;
                        _ClientCertErrors = default(Exception);
                        // This can return true incorrectly for values that delayed initialization may determine are not actually present.
                        return true;
                    }
                   break;
            }
            return false;
        }

        private IEnumerable<string> PropertiesKeys()
        {
            if (((_flag0 & 0x1u) != 0))
            {
                yield return "owin.RequestPath";
            }
            if (((_flag0 & 0x2u) != 0))
            {
                yield return "owin.ResponseHeaders";
            }
            if (((_flag0 & 0x4u) != 0))
            {
                yield return "owin.RequestHeaders";
            }
            if (((_flag0 & 0x8u) != 0))
            {
                yield return "owin.ResponseBody";
            }
            if (((_flag0 & 0x10u) != 0))
            {
                yield return "owin.RequestBody";
            }
            if (((_flag0 & 0x20u) != 0))
            {
                yield return "owin.RequestId";
            }
            if (((_flag0 & 0x40u) != 0))
            {
                yield return "owin.ResponseStatusCode";
            }
            if (((_flag0 & 0x80u) != 0))
            {
                yield return "owin.ResponseReasonPhrase";
            }
            if (((_flag0 & 0x100u) != 0))
            {
                yield return "owin.RequestQueryString";
            }
            if (((_flag0 & 0x200u) != 0))
            {
                yield return "owin.CallCancelled";
            }
            if (((_flag0 & 0x400u) != 0))
            {
                yield return "owin.RequestMethod";
            }
            if (((_flag0 & 0x800u) != 0))
            {
                yield return "owin.RequestScheme";
            }
            if (((_flag0 & 0x1000u) != 0))
            {
                yield return "owin.RequestPathBase";
            }
            if (((_flag0 & 0x2000u) != 0))
            {
                yield return "owin.RequestProtocol";
            }
            if (((_flag0 & 0x4000u) != 0))
            {
                yield return "owin.Version";
            }
            if (((_flag0 & 0x8000u) != 0))
            {
                yield return "host.TraceOutput";
            }
            if (((_flag0 & 0x10000u) != 0))
            {
                yield return "host.AppName";
            }
            if (((_flag0 & 0x20000u) != 0))
            {
                yield return "host.AppMode";
            }
            if (((_flag0 & 0x40000u) != 0))
            {
                yield return "host.OnAppDisposing";
            }
            if (((_flag0 & 0x80000u) != 0))
            {
                yield return "server.User";
            }
            if (((_flag0 & 0x100000u) != 0))
            {
                yield return "server.OnSendingHeaders";
            }
            if (((_flag0 & 0x200000u) != 0))
            {
                yield return "server.Capabilities";
            }
            if (((_flag0 & 0x400000u) != 0))
            {
                yield return "server.RemoteIpAddress";
            }
            if (((_flag0 & 0x800000u) != 0))
            {
                yield return "server.RemotePort";
            }
            if (((_flag0 & 0x1000000u) != 0))
            {
                yield return "server.LocalIpAddress";
            }
            if (((_flag0 & 0x2000000u) != 0))
            {
                yield return "server.LocalPort";
            }
            if (((_flag0 & 0x4000000u) != 0))
            {
                yield return "server.IsLocal";
            }
            if (((_flag0 & 0x8000000u) != 0))
            {
                if (((_initFlag0 & 0x8000000u) == 0) || InitPropertyClientCert())
                {
                    yield return "ssl.ClientCertificate";
                }
            }
            if (((_flag0 & 0x10000000u) != 0))
            {
                if (((_initFlag0 & 0x10000000u) == 0) || InitPropertyClientCertErrors())
                {
                    yield return "ssl.ClientCertificateErrors";
                }
            }
            if (((_flag0 & 0x20000000u) != 0))
            {
                yield return "ssl.LoadClientCertAsync";
            }
            if (((_flag0 & 0x40000000u) != 0))
            {
                if (((_initFlag0 & 0x40000000u) == 0) || InitPropertyWebSocketAccept())
                {
                    yield return "websocket.Accept";
                }
            }
            if (((_flag0 & 0x80000000u) != 0))
            {
                yield return "sendfile.SendAsync";
            }
            if (((_flag1 & 0x1u) != 0))
            {
                yield return HttpListenerContextName;
            }
        }

        private IEnumerable<object> PropertiesValues()
        {
            if (((_flag0 & 0x1u) != 0))
            {
                yield return RequestPath;
            }
            if (((_flag0 & 0x2u) != 0))
            {
                yield return ResponseHeaders;
            }
            if (((_flag0 & 0x4u) != 0))
            {
                yield return RequestHeaders;
            }
            if (((_flag0 & 0x8u) != 0))
            {
                yield return ResponseBody;
            }
            if (((_flag0 & 0x10u) != 0))
            {
                yield return RequestBody;
            }
            if (((_flag0 & 0x20u) != 0))
            {
                yield return RequestId;
            }
            if (((_flag0 & 0x40u) != 0))
            {
                yield return ResponseStatusCode;
            }
            if (((_flag0 & 0x80u) != 0))
            {
                yield return ResponseReasonPhrase;
            }
            if (((_flag0 & 0x100u) != 0))
            {
                yield return RequestQueryString;
            }
            if (((_flag0 & 0x200u) != 0))
            {
                yield return CallCancelled;
            }
            if (((_flag0 & 0x400u) != 0))
            {
                yield return RequestMethod;
            }
            if (((_flag0 & 0x800u) != 0))
            {
                yield return RequestScheme;
            }
            if (((_flag0 & 0x1000u) != 0))
            {
                yield return RequestPathBase;
            }
            if (((_flag0 & 0x2000u) != 0))
            {
                yield return RequestProtocol;
            }
            if (((_flag0 & 0x4000u) != 0))
            {
                yield return OwinVersion;
            }
            if (((_flag0 & 0x8000u) != 0))
            {
                yield return HostTraceOutput;
            }
            if (((_flag0 & 0x10000u) != 0))
            {
                yield return HostAppName;
            }
            if (((_flag0 & 0x20000u) != 0))
            {
                yield return HostAppMode;
            }
            if (((_flag0 & 0x40000u) != 0))
            {
                yield return OnAppDisposing;
            }
            if (((_flag0 & 0x80000u) != 0))
            {
                yield return ServerUser;
            }
            if (((_flag0 & 0x100000u) != 0))
            {
                yield return OnSendingHeaders;
            }
            if (((_flag0 & 0x200000u) != 0))
            {
                yield return ServerCapabilities;
            }
            if (((_flag0 & 0x400000u) != 0))
            {
                yield return ServerRemoteIpAddress;
            }
            if (((_flag0 & 0x800000u) != 0))
            {
                yield return ServerRemotePort;
            }
            if (((_flag0 & 0x1000000u) != 0))
            {
                yield return ServerLocalIpAddress;
            }
            if (((_flag0 & 0x2000000u) != 0))
            {
                yield return ServerLocalPort;
            }
            if (((_flag0 & 0x4000000u) != 0))
            {
                yield return ServerIsLocal;
            }
            if (((_flag0 & 0x8000000u) != 0))
            {
                if (((_initFlag0 & 0x8000000u) == 0) || InitPropertyClientCert())
                {
                    yield return ClientCert;
                }
            }
            if (((_flag0 & 0x10000000u) != 0))
            {
                if (((_initFlag0 & 0x10000000u) == 0) || InitPropertyClientCertErrors())
                {
                    yield return ClientCertErrors;
                }
            }
            if (((_flag0 & 0x20000000u) != 0))
            {
                yield return LoadClientCert;
            }
//            if (((_flag0 & 0x40000000u) != 0))
//            {
//                if (((_initFlag0 & 0x40000000u) == 0) || InitPropertyWebSocketAccept())
//                {
//                    yield return WebSocketAccept;
//                }
//            }
//            if (((_flag0 & 0x80000000u) != 0))
//            {
//                yield return SendFileAsync;
//            }
//            if (((_flag1 & 0x1u) != 0))
//            {
//                yield return RequestContext;
//            }
//            if (((_flag1 & 0x2u) != 0))
//            {
//                yield return Listener;
//            }
//            if (((_flag1 & 0x4u) != 0))
//            {
//                yield return OwinWebListener;
//            }
        }

        private IEnumerable<KeyValuePair<string, object>> PropertiesEnumerable()
        {
            if (((_flag0 & 0x1u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.RequestPath", RequestPath);
            }
            if (((_flag0 & 0x2u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.ResponseHeaders", ResponseHeaders);
            }
            if (((_flag0 & 0x4u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.RequestHeaders", RequestHeaders);
            }
            if (((_flag0 & 0x8u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.ResponseBody", ResponseBody);
            }
            if (((_flag0 & 0x10u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.RequestBody", RequestBody);
            }
            if (((_flag0 & 0x20u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.RequestId", RequestId);
            }
            if (((_flag0 & 0x40u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.ResponseStatusCode", ResponseStatusCode);
            }
            if (((_flag0 & 0x80u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.ResponseReasonPhrase", ResponseReasonPhrase);
            }
            if (((_flag0 & 0x100u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.RequestQueryString", RequestQueryString);
            }
            if (((_flag0 & 0x200u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.CallCancelled", CallCancelled);
            }
            if (((_flag0 & 0x400u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.RequestMethod", RequestMethod);
            }
            if (((_flag0 & 0x800u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.RequestScheme", RequestScheme);
            }
            if (((_flag0 & 0x1000u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.RequestPathBase", RequestPathBase);
            }
            if (((_flag0 & 0x2000u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.RequestProtocol", RequestProtocol);
            }
            if (((_flag0 & 0x4000u) != 0))
            {
                yield return new KeyValuePair<string, object>("owin.Version", OwinVersion);
            }
            if (((_flag0 & 0x8000u) != 0))
            {
                yield return new KeyValuePair<string, object>("host.TraceOutput", HostTraceOutput);
            }
            if (((_flag0 & 0x10000u) != 0))
            {
                yield return new KeyValuePair<string, object>("host.AppName", HostAppName);
            }
            if (((_flag0 & 0x20000u) != 0))
            {
                yield return new KeyValuePair<string, object>("host.AppMode", HostAppMode);
            }
            if (((_flag0 & 0x40000u) != 0))
            {
                yield return new KeyValuePair<string, object>("host.OnAppDisposing", OnAppDisposing);
            }
            if (((_flag0 & 0x80000u) != 0))
            {
                yield return new KeyValuePair<string, object>("server.User", ServerUser);
            }
            if (((_flag0 & 0x100000u) != 0))
            {
                yield return new KeyValuePair<string, object>("server.OnSendingHeaders", OnSendingHeaders);
            }
            if (((_flag0 & 0x200000u) != 0))
            {
                yield return new KeyValuePair<string, object>("server.Capabilities", ServerCapabilities);
            }
            if (((_flag0 & 0x400000u) != 0))
            {
                yield return new KeyValuePair<string, object>("server.RemoteIpAddress", ServerRemoteIpAddress);
            }
            if (((_flag0 & 0x800000u) != 0))
            {
                yield return new KeyValuePair<string, object>("server.RemotePort", ServerRemotePort);
            }
            if (((_flag0 & 0x1000000u) != 0))
            {
                yield return new KeyValuePair<string, object>("server.LocalIpAddress", ServerLocalIpAddress);
            }
            if (((_flag0 & 0x2000000u) != 0))
            {
                yield return new KeyValuePair<string, object>("server.LocalPort", ServerLocalPort);
            }
            if (((_flag0 & 0x4000000u) != 0))
            {
                yield return new KeyValuePair<string, object>("server.IsLocal", ServerIsLocal);
            }
            if (((_flag0 & 0x8000000u) != 0))
            {
                if (((_initFlag0 & 0x8000000u) == 0) || InitPropertyClientCert())
                {
                    yield return new KeyValuePair<string, object>("ssl.ClientCertificate", ClientCert);
                }
            }
            if (((_flag0 & 0x10000000u) != 0))
            {
                if (((_initFlag0 & 0x10000000u) == 0) || InitPropertyClientCertErrors())
                {
                    yield return new KeyValuePair<string, object>("ssl.ClientCertificateErrors", ClientCertErrors);
                }
            }
            if (((_flag0 & 0x20000000u) != 0))
            {
                yield return new KeyValuePair<string, object>("ssl.LoadClientCertAsync", LoadClientCert);
            }
//            if (((_flag0 & 0x40000000u) != 0))
//            {
//                if (((_initFlag0 & 0x40000000u) == 0) || InitPropertyWebSocketAccept())
//                {
//                    yield return new KeyValuePair<string, object>("websocket.Accept", WebSocketAccept);
//                }
//            }
            if (((_flag0 & 0x80000000u) != 0))
            {
                yield return new KeyValuePair<string, object>("sendfile.SendAsync", SendFileAsync);
            }
            if (((_flag1 & 0x1u) != 0))
            {
                yield return new KeyValuePair<string, object>(HttpListenerContextName, RequestContext);
            }
        }
    }
}