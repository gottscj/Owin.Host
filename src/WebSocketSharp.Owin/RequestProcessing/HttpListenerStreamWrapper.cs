using System;
using System.IO;
using WebSocketSharp.Owin.WebSocketSharp.Net;

namespace WebSocketSharp.Owin.RequestProcessing
{
    internal class HttpListenerStreamWrapper : ExceptionFilterStream
    {
        internal HttpListenerStreamWrapper(Stream innerStream)
            : base(innerStream)
        {
        }

        // Convert HttpListenerExceptions to IOExceptions
        protected override bool TryWrapException(Exception ex, out Exception wrapped)
        {
            if (ex is HttpListenerException)
            {
                wrapped = new IOException(string.Empty, ex);
                return true;
            }

            wrapped = null;
            return false;
        }

        public override void Close()
        {
            // Disabled. The server will close the response when the AppFunc task completes.
        }

        protected override void Dispose(bool disposing)
        {
            // Disabled. The server will close the response when the AppFunc task completes.
        }
    }
}