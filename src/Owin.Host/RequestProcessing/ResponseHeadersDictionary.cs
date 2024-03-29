using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gottscj.Owin.Host.SocketHttpListener;
using Gottscj.Owin.Host.SocketHttpListener.Net;

namespace Gottscj.Owin.Host.RequestProcessing
{
   // This class exposes the response headers collection as a mutable dictionary, and re-maps restricted headers
    // to their associated properties.
    internal sealed class ResponseHeadersDictionary : HeadersDictionaryBase
    {
        private readonly HttpListenerResponse _response;

        internal ResponseHeadersDictionary(HttpListenerResponse response)
            : base()
        {
            _response = response;
            Headers = _response.Headers;
        }

        private bool HasContentLength => _response.ContentLength64 > 0;

        private string[] ContentLength => new[]
        {
            _response.ContentLength64.ToString(CultureInfo.InvariantCulture)
        };

        public override ICollection<string> Keys
        {
            get
            {
                if (HasContentLength)
                {
                    return Ext.ToList(base.Keys.Concat(new[] { Constants.ContentLengthHeader }));
                }

                return base.Keys;
            }
        }

        public override bool TryGetValue(string header, out string[] value)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (header.Equals(Constants.ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
            {
                if (HasContentLength)
                {
                    value = ContentLength;
                    return true;
                }
            }
            return base.TryGetValue(header, out value);
        }

        protected override string[] Get(string header)
        {
            if (header.Equals(Constants.ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
            {
                if (HasContentLength)
                {
                    return ContentLength;
                }
            }
            return base.Get(header);
        }

        protected override void Set(string header, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // Some header values are restricted
            if (header.Equals(Constants.ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
            {
                _response.ContentLength64 = long.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (header.Equals(Constants.TransferEncodingHeader, StringComparison.OrdinalIgnoreCase)
                && value.Equals("chunked", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: what about a mixed format value like chunked, otherTransferEncoding?
                //_response.SendChunked = true;
            }
            else if (header.Equals(Constants.ConnectionHeader, StringComparison.OrdinalIgnoreCase)
                && value.Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                //_response.KeepAlive = false;
            }
            else if (header.Equals(Constants.KeepAliveHeader, StringComparison.OrdinalIgnoreCase)
                && value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                // HTTP/1.0 semantics
                //_response.KeepAlive = true;
            }
            else if (header.Equals(Constants.WwwAuthenticateHeader, StringComparison.OrdinalIgnoreCase))
            {
                // WWW-Authenticate is restricted and must use Response.AddHeader with a single merged value.
                // Uses SetInternal to bypass a response header restriction.
                _response.Headers.Add(Constants.WwwAuthenticateHeader, value);
            }
            else
            {
                base.Set(header, value);
            }
        }

        protected override void Append(string header, string value)
        {
            // header was already validated.
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // Some header values are restricted
            if (header.Equals(Constants.ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
            {
                _response.ContentLength64 = long.Parse(value, CultureInfo.InvariantCulture);
            }
            else if (header.Equals(Constants.TransferEncodingHeader, StringComparison.OrdinalIgnoreCase)
                && value.Equals("chunked", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: what about a mixed format value like chunked, otherTransferEncoding?
                //_response.SendChunked = true;
            }
            else if (header.Equals(Constants.ConnectionHeader, StringComparison.OrdinalIgnoreCase)
                && value.Equals("close", StringComparison.OrdinalIgnoreCase))
            {
               // _response.KeepAlive = false;
            }
            else if (header.Equals(Constants.KeepAliveHeader, StringComparison.OrdinalIgnoreCase)
                && value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                // HTTP/1.0 semantics
                //_response.KeepAlive = true;
            }
            else if (header.Equals(Constants.WwwAuthenticateHeader, StringComparison.OrdinalIgnoreCase))
            {
                // WWW-Authenticate is restricted and must use Response.AddHeader with a single 
                // merged value.  See CopyResponseHeaders.
                if (ContainsKey(Constants.WwwAuthenticateHeader))
                {
                    string[] wwwAuthValues = Get(Constants.WwwAuthenticateHeader);
                    var newHeader = new string[wwwAuthValues.Length + 1];
                    wwwAuthValues.CopyTo(newHeader, 0);
                    newHeader[newHeader.Length - 1] = value;

                    // Uses InternalAdd to bypass a response header restriction, but to do so we must merge the values.
                    _response.Headers.Add(Constants.WwwAuthenticateHeader, string.Join(", ", newHeader));
                }
                else
                {
                    // Uses InternalAdd to bypass a response header restriction, but to do so we must merge the values.
                    _response.Headers.Add(Constants.WwwAuthenticateHeader, value);
                }
            }
            else
            {
                base.Append(header, value);
            }
        }

        public override bool Remove(string header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            if (!ContainsKey(header))
            {
                return false;
            }

            // Some header values are restricted
            if (header.Equals(Constants.ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
            {
                _response.ContentLength64 = 0;
            }
            else if (header.Equals(Constants.TransferEncodingHeader, StringComparison.OrdinalIgnoreCase))
            {
                // TODO: what about a mixed format value like chunked, otherTransferEncoding?
                //_response.SendChunked = false;
            }
            else if (header.Equals(Constants.ConnectionHeader, StringComparison.OrdinalIgnoreCase))
            {
                //_response.KeepAlive = true;
            }
            else if (header.Equals(Constants.KeepAliveHeader, StringComparison.OrdinalIgnoreCase))
            {
                // HTTP/1.0 semantics
                //_response.KeepAlive = false;
            }
            else if (header.Equals(Constants.WwwAuthenticateHeader, StringComparison.OrdinalIgnoreCase))
            {
                // Can't be completely removed, but can be overwritten.
                _response.Headers.Add(Constants.WwwAuthenticateHeader, string.Empty);
            }
            else
            {
                return base.Remove(header);
            }
            return true;
        }

        protected override void RemoveSilent(string header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            // Some header values are restricted
            if (header.Equals(Constants.ContentLengthHeader, StringComparison.OrdinalIgnoreCase))
            {
                _response.ContentLength64 = 0;
            }
            else if (header.Equals(Constants.TransferEncodingHeader, StringComparison.OrdinalIgnoreCase))
            {
                // TODO: what about a mixed format value like chunked, otherTransferEncoding?
                //_response.SendChunked = false;
            }
            else if (header.Equals(Constants.ConnectionHeader, StringComparison.OrdinalIgnoreCase))
            {
                //_response.KeepAlive = true;
            }
            else if (header.Equals(Constants.KeepAliveHeader, StringComparison.OrdinalIgnoreCase))
            {
                // HTTP/1.0 semantics
                //_response.KeepAlive = false;
            }
            else if (header.Equals(Constants.WwwAuthenticateHeader, StringComparison.OrdinalIgnoreCase))
            {
                // Can't be completely removed, but can be overwritten.
                _response.Headers.Add(Constants.WwwAuthenticateHeader, string.Empty);
            }
            else
            {
                base.RemoveSilent(header);
            }
        }

        public override IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            if (HasContentLength)
            {
                yield return new KeyValuePair<string, string[]>(Constants.ContentLengthHeader, ContentLength);
            }

            foreach (var key in Headers.AllKeys)
            {
                yield return new KeyValuePair<string, string[]>(key, Headers.GetValues(key)?.ToArray() ?? new string[0]);
            }
        }
    }
}