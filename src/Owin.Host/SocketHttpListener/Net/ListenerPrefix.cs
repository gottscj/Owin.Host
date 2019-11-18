using System;
using System.Net;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    internal sealed class ListenerPrefix
    {
        private readonly string _original;
        private string _host;
        private ushort _port;
        private string _path;
        private bool _secure;
        private IPAddress[] _addresses;
        public HttpListener Listener;

        public ListenerPrefix(string prefix)
        {
            _original = prefix;
            Parse(prefix);
        }

        public override string ToString()
        {
            return _original;
        }

        public IPAddress[] Addresses
        {
            get => _addresses;
            set => _addresses = value;
        }
        public bool Secure => _secure;

        public string Host => _host;

        public int Port => (int)_port;

        public string Path => _path;

        // Equals and GetHashCode are required to detect duplicates in HttpListenerPrefixCollection.
        public override bool Equals(object o)
        {
            ListenerPrefix other = o as ListenerPrefix;
            if (other == null)
                return false;

            return (_original == other._original);
        }

        public override int GetHashCode()
        {
            return _original.GetHashCode();
        }

        private void Parse(string uri)
        {
            ushort defaultPort = 80;
            if (uri.StartsWith("https://"))
            {
                defaultPort = 443;
                _secure = true;
            }

            int length = uri.Length;
            int startHost = uri.IndexOf(':') + 3;
            if (startHost >= length)
                throw new ArgumentException("No host specified.");

            int colon = uri.IndexOf(':', startHost, length - startHost);
            int root;
            if (colon > 0)
            {
                _host = uri.Substring(startHost, colon - startHost);
                root = uri.IndexOf('/', colon, length - colon);
                _port = (ushort)Int32.Parse(uri.Substring(colon + 1, root - colon - 1));
                _path = uri.Substring(root);
            }
            else
            {
                root = uri.IndexOf('/', startHost, length - startHost);
                _host = uri.Substring(startHost, root - startHost);
                _port = defaultPort;
                _path = uri.Substring(root);
            }
            if (_path.Length != 1)
                _path = _path.Substring(0, _path.Length - 1);
        }

        public static void CheckUri(string uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uriPrefix");

            if (!uri.StartsWith("http://") && !uri.StartsWith("https://"))
                throw new ArgumentException("Only 'http' and 'https' schemes are supported.");

            int length = uri.Length;
            int startHost = uri.IndexOf(':') + 3;
            if (startHost >= length)
                throw new ArgumentException("No host specified.");

            int colon = uri.IndexOf(':', startHost, length - startHost);
            if (startHost == colon)
                throw new ArgumentException("No host specified.");

            int root;
            if (colon > 0)
            {
                root = uri.IndexOf('/', colon, length - colon);
                if (root == -1)
                    throw new ArgumentException("No path specified.");

                try
                {
                    int p = Int32.Parse(uri.Substring(colon + 1, root - colon - 1));
                    if (p <= 0 || p >= 65536)
                        throw new Exception();
                }
                catch
                {
                    throw new ArgumentException("Invalid port.");
                }
            }
            else
            {
                root = uri.IndexOf('/', startHost, length - startHost);
                if (root == -1)
                    throw new ArgumentException("No path specified.");
            }

            if (uri[uri.Length - 1] != '/')
                throw new ArgumentException("The prefix must end with '/'");
        }
    }
}
