using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using HttpStatusCode = Gottscj.Owin.Host.SocketHttpListener.Net.HttpStatusCode;
using HttpVersion = Gottscj.Owin.Host.SocketHttpListener.Net.HttpVersion;

namespace Gottscj.Owin.Host.SocketHttpListener
{
    internal class HttpResponse : HttpBase
    {
        #region Private Fields

        private readonly string _code;
        private readonly string _reason;

        #endregion

        #region Private Constructors

        private HttpResponse(string code, string reason, Version version, NameValueCollection headers)
            : base(version, headers)
        {
            _code = code;
            _reason = reason;
        }

        #endregion

        #region Internal Constructors

        internal HttpResponse(HttpStatusCode code)
            : this(code, code.GetDescription())
        {
        }

        internal HttpResponse(HttpStatusCode code, string reason)
            : this(((int)code).ToString(), reason, HttpVersion.Version11, new NameValueCollection())
        {
            Headers["Server"] = "websocket-sharp/1.0";
        }

        #endregion

        #region Public Properties

        public CookieCollection Cookies => Headers.GetCookies(true);

        public bool IsProxyAuthenticationRequired => _code == "407";

        public bool IsUnauthorized => _code == "401";

        public bool IsWebSocketResponse
        {
            get
            {
                var headers = Headers;
                return ProtocolVersion > HttpVersion.Version10 &&
                       _code == "101" &&
                       headers.Contains("Upgrade", "websocket") &&
                       headers.Contains("Connection", "Upgrade");
            }
        }

        public string Reason => _reason;

        public string StatusCode => _code;

        #endregion

        #region Internal Methods

        internal static HttpResponse CreateCloseResponse(HttpStatusCode code)
        {
            var res = new HttpResponse(code);
            res.Headers["Connection"] = "close";

            return res;
        }

        internal static HttpResponse CreateWebSocketResponse()
        {
            var res = new HttpResponse(HttpStatusCode.SwitchingProtocols);

            var headers = res.Headers;
            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";

            return res;
        }

        #endregion

        #region Public Methods

        public void SetCookies(CookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return;

            var headers = Headers;
            var sorted = cookies.OfType<Cookie>().OrderBy(i => i.Name).ToList();

            foreach (var cookie in sorted)
                headers.Add("Set-Cookie", cookie.ToString());
        }

        public override string ToString()
        {
            var output = new StringBuilder(64);
            output.AppendFormat("HTTP/{0} {1} {2}{3}", ProtocolVersion, _code, _reason, CrLf);

            var headers = Headers;
            foreach (var key in headers.AllKeys)
                output.AppendFormat("{0}: {1}{2}", key, headers[key], CrLf);

            output.Append(CrLf);

            var entity = EntityBody;
            if (entity.Length > 0)
                output.Append(entity);

            return output.ToString();
        }

        #endregion
    }
}