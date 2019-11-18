using System;
using System.Collections;
using System.Collections.Generic;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    public class HttpListenerPrefixCollection : ICollection<string>, IEnumerable<string>, IEnumerable
    {
        private readonly List<string> _prefixes = new List<string>();
        private readonly HttpListener _listener;

        private readonly ILogger _logger;

        internal HttpListenerPrefixCollection(ILogger logger, HttpListener listener)
        {
            _logger = logger;
            _listener = listener;
        }

        public int Count => _prefixes.Count;

        public bool IsReadOnly => false;

        public bool IsSynchronized => false;

        public void Add(string uriPrefix)
        {
            _listener.CheckDisposed();
            ListenerPrefix.CheckUri(uriPrefix);
            if (_prefixes.Contains(uriPrefix))
                return;

            _prefixes.Add(uriPrefix);
            if (_listener.IsListening)
                EndPointManager.AddPrefix(_logger, uriPrefix, _listener);
        }

        public void Clear()
        {
            _listener.CheckDisposed();
            _prefixes.Clear();
            if (_listener.IsListening)
                EndPointManager.RemoveListener(_logger, _listener);
        }

        public bool Contains(string uriPrefix)
        {
            _listener.CheckDisposed();
            return _prefixes.Contains(uriPrefix);
        }

        public void CopyTo(string[] array, int offset)
        {
            _listener.CheckDisposed();
            _prefixes.CopyTo(array, offset);
        }

        public void CopyTo(Array array, int offset)
        {
            _listener.CheckDisposed();
            ((ICollection)_prefixes).CopyTo(array, offset);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _prefixes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _prefixes.GetEnumerator();
        }

        public bool Remove(string uriPrefix)
        {
            _listener.CheckDisposed();
            if (uriPrefix == null)
                throw new ArgumentNullException(nameof(uriPrefix));

            bool result = _prefixes.Remove(uriPrefix);
            if (result && _listener.IsListening)
                EndPointManager.RemovePrefix(_logger, uriPrefix, _listener);

            return result;
        }
    }
}
