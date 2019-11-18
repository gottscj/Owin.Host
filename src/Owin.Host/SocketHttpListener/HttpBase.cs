using System;
using System.Collections.Specialized;
using System.Text;
#pragma warning disable 649

namespace Gottscj.Owin.Host.SocketHttpListener
{
    internal abstract class HttpBase
    {
        #region Private Fields

        private readonly NameValueCollection _headers;
        private readonly Version _version;

        #endregion

        #region Internal Fields

        internal byte[] EntityBodyData;

        #endregion

        #region Protected Fields

        protected const string CrLf = "\r\n";

        #endregion

        #region Protected Constructors

        protected HttpBase(Version version, NameValueCollection headers)
        {
            _version = version;
            _headers = headers;
        }

        #endregion

        #region Public Properties

        public string EntityBody =>
            EntityBodyData != null && EntityBodyData.Length > 0
                ? GetEncoding(_headers["Content-Type"]).GetString(EntityBodyData)
                : String.Empty;

        public NameValueCollection Headers => _headers;

        public Version ProtocolVersion => _version;

        #endregion

        #region Private Methods

        private static Encoding GetEncoding(string contentType)
        {
            if (contentType == null || contentType.Length == 0)
                return Encoding.UTF8;

            var i = contentType.IndexOf("charset=", StringComparison.Ordinal);
            if (i == -1)
                return Encoding.UTF8;

            var charset = contentType.Substring(i + 8);
            i = charset.IndexOf(';');
            if (i != -1)
                charset = charset.Substring(0, i).TrimEnd();

            return Encoding.GetEncoding(charset.Trim('"'));
        }

        #endregion

        #region Public Methods

        public byte[] ToByteArray()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }

        #endregion
    }
}