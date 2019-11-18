// TODO: Logging.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    public sealed class HttpListener : IDisposable
    {
        private AuthenticationSchemes _authSchemes;
        private readonly HttpListenerPrefixCollection _prefixes;
        private AuthenticationSchemeSelector _authSelector;
        private string _realm;
        private bool _ignoreWriteExceptions;
        private bool _unsafeNtlmAuth;
        private bool _listening;
        private bool _disposed;

        private readonly Dictionary<HttpListenerContext, HttpListenerContext> _registry;   // Dictionary<HttpListenerContext,HttpListenerContext> 
        private readonly Dictionary<HttpConnection, HttpConnection> _connections;
        private readonly ILogger _logger;
        private X509Certificate2 _certificate;

        public Action<HttpListenerContext> OnContext { get; set; }

        public HttpListener()
            : this(new NullLogger())
        {
        }

        public HttpListener(ILogger logger)
        {
            _logger = logger;
            _prefixes = new HttpListenerPrefixCollection(logger, this);
            _registry = new Dictionary<HttpListenerContext, HttpListenerContext>();
            _connections = new Dictionary<HttpConnection, HttpConnection>();
            _authSchemes = AuthenticationSchemes.Anonymous;
        }

        public HttpListener(X509Certificate2 certificate)
            :this(new NullLogger(), certificate)
        {
        }

        public HttpListener(string certificateLocation)
            : this(new NullLogger(), certificateLocation)
        {
        }

        public HttpListener(ILogger logger, X509Certificate2 certificate)
            : this(logger)
        {
            _certificate = certificate;
        }

        public HttpListener(ILogger logger, string certificateLocation)
            :this(logger)
        {
            LoadCertificateAndKey(certificateLocation);
        }

        // TODO: Digest, NTLM and Negotiate require ControlPrincipal
        public AuthenticationSchemes AuthenticationSchemes
        {
            get => _authSchemes;
            set
            {
                CheckDisposed();
                _authSchemes = value;
            }
        }

        public AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate
        {
            get => _authSelector;
            set
            {
                CheckDisposed();
                _authSelector = value;
            }
        }

        public bool IgnoreWriteExceptions
        {
            get => _ignoreWriteExceptions;
            set
            {
                CheckDisposed();
                _ignoreWriteExceptions = value;
            }
        }

        public bool IsListening => _listening;

        public static bool IsSupported => true;

        public HttpListenerPrefixCollection Prefixes
        {
            get
            {
                CheckDisposed();
                return _prefixes;
            }
        }

        // TODO: use this
        public string Realm
        {
            get => _realm;
            set
            {
                CheckDisposed();
                _realm = value;
            }
        }

        public bool UnsafeConnectionNtlmAuthentication
        {
            get => _unsafeNtlmAuth;
            set
            {
                CheckDisposed();
                _unsafeNtlmAuth = value;
            }
        }

        private void LoadCertificateAndKey(string certificateLocation)
        {
            // Actually load the certificate
            try
            {
                _logger.Info("attempting to load pfx: {0}", certificateLocation);
                if (!File.Exists(certificateLocation))
                {
                    _logger.Error("Secure requested, but no certificate found at: {0}", certificateLocation);
                    return;
                }

                X509Certificate2 localCert = new X509Certificate2(certificateLocation);
                //localCert.PrivateKey = PrivateKey.CreateFromFile(pvk_file).RSA;
                if (localCert.PrivateKey == null)
                {
                    _logger.Error("Secure requested, no private key included in: {0}", certificateLocation);
                    return;
                }

                _certificate = localCert;
            }
            catch (Exception e)
            {
                _logger.ErrorException("Exception loading certificate: {0}", e, certificateLocation ?? "<NULL>");
                // ignore errors
            }
        }

        //internal IMonoSslStream CreateSslStream(Stream innerStream, bool ownsStream, MSI.MonoRemoteCertificateValidationCallback callback)
        //{
        //    lock (registry)
        //    {
        //        if (tlsProvider == null)
        //            tlsProvider = MonoTlsProviderFactory.GetProviderInternal();
        //        if (tlsSettings == null)
        //            tlsSettings = MSI.MonoTlsSettings.CopyDefaultSettings();
        //        if (tlsSettings.RemoteCertificateValidationCallback == null)
        //            tlsSettings.RemoteCertificateValidationCallback = callback;
        //        return tlsProvider.CreateSslStream(innerStream, ownsStream, tlsSettings);
        //    }
        //}

        internal X509Certificate2 Certificate => _certificate;

        public void Abort()
        {
            if (_disposed)
                return;

            if (!_listening)
            {
                return;
            }

            Close(true);
        }

        public void Close()
        {
            if (_disposed)
                return;

            if (!_listening)
            {
                _disposed = true;
                return;
            }

            Close(true);
            _disposed = true;
        }

        private void Close(bool force)
        {
            CheckDisposed();
            EndPointManager.RemoveListener(_logger, this);
            Cleanup(force);
        }

        private void Cleanup(bool closeExisting)
        {
            lock (_registry)
            {
                if (closeExisting)
                {
                    // Need to copy this since closing will call UnregisterContext
                    ICollection keys = _registry.Keys;
                    var all = new HttpListenerContext[keys.Count];
                    keys.CopyTo(all, 0);
                    _registry.Clear();
                    for (int i = all.Length - 1; i >= 0; i--)
                        all[i].Connection.Close(true);
                }

                lock (_connections)
                {
                    ICollection keys = _connections.Keys;
                    var conns = new HttpConnection[keys.Count];
                    keys.CopyTo(conns, 0);
                    _connections.Clear();
                    for (int i = conns.Length - 1; i >= 0; i--)
                        conns[i].Close(true);
                }
            }
        }

        internal AuthenticationSchemes SelectAuthenticationScheme(HttpListenerContext context)
        {
            if (AuthenticationSchemeSelectorDelegate != null)
                return AuthenticationSchemeSelectorDelegate(context.Request);
            else
                return _authSchemes;
        }

        public void Start()
        {
            CheckDisposed();
            if (_listening)
                return;

            EndPointManager.AddListener(_logger, this);
            _listening = true;
        }

        public void Stop()
        {
            CheckDisposed();
            _listening = false;
            Close(false);
        }

        void IDisposable.Dispose()
        {
            if (_disposed)
                return;

            Close(true); //TODO: Should we force here or not?
            _disposed = true;
        }

        internal void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());
        }

        internal void RegisterContext(HttpListenerContext context)
        {
            if (OnContext != null && IsListening)
            {
                OnContext(context);
            }

            lock (_registry)
                _registry[context] = context;
        }

        internal void UnregisterContext(HttpListenerContext context)
        {
            lock (_registry)
                _registry.Remove(context);
        }

        internal void AddConnection(HttpConnection cnc)
        {
            lock (_connections)
            {
                _connections[cnc] = cnc;
            }
        }

        internal void RemoveConnection(HttpConnection cnc)
        {
            lock (_connections)
            {
                _connections.Remove(cnc);
            }
        }
    }
}
