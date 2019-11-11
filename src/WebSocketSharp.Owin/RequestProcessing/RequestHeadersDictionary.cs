using System;
using System.Collections.Specialized;
using System.Globalization;
using SocketHttpListener.Net;

namespace WebSocketSharp.Owin.RequestProcessing
{
    /// <summary>
    /// This wraps HttpListenerRequest's WebHeaderCollection (NameValueCollection) and adapts it to 
    /// the OWIN required IDictionary surface area. It remains fully mutable, but you will be subject 
    /// to the header validations performed by the underlying collection.
    /// </summary>
    internal sealed class RequestHeadersDictionary : HeadersDictionaryBase
    {
        private readonly HttpListenerRequest _request;

        internal RequestHeadersDictionary(HttpListenerRequest request)
            : base()
        {
            _request = request;
        }

        // This override enables delay load of headers
        protected override NameValueCollection Headers
        {
            get { return _request.Headers; }
            set { throw new InvalidOperationException(); }
        }

        // For known headers, access them via property to prevent loading the entire headers collection
        // The following are 'Known' by HttpListener/Http.Sys, but for now we've only optimized the most common ones.
        // Accept
        // Transfer-Encoding
        // Content-Type
        // Content-Length
        // Cookie
        // Connection
        // Keep-Alive
        // Referer
        // UserAgent
        // Host
        // Accept-Language
        public override bool TryGetValue(string key, out string[] value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                value = null;
                return false;
            }

            if (key.Equals(Constants.ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
            {
                long contentLength = _request.ContentLength64;
                if (contentLength >= 0)
                {
                    value = new[] { contentLength.ToString(CultureInfo.InvariantCulture) };
                    return true;
                }
            }
            else if (key.Equals(Constants.HostHeader, StringComparison.OrdinalIgnoreCase))
            {
                string host = null; // TODO _request.UserHostName;
                if (host != null)
                {
                    value = new[] { host };
                    return true;
                }
            }

            return base.TryGetValue(key, out value);
        }
    }
}