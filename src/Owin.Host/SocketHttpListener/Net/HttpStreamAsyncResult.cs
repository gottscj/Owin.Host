using System;
using System.Threading;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    internal class HttpStreamAsyncResult : IAsyncResult
    {
        private readonly object _locker = new object();
        private ManualResetEvent _handle;
        private bool _completed;

        internal byte[] Buffer;
        internal int Offset;
        internal int Count;
        internal AsyncCallback Callback;
        internal object State;
        internal int SynchRead;
        internal Exception Error;

        public void Complete(Exception e)
        {
            Error = e;
            Complete();
        }

        public void Complete()
        {
            lock (_locker)
            {
                if (_completed)
                    return;

                _completed = true;
                if (_handle != null)
                    _handle.Set();

                if (Callback != null)
                    Callback.BeginInvoke(this, null, null);
            }
        }

        public object AsyncState => State;

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock (_locker)
                {
                    if (_handle == null)
                        _handle = new ManualResetEvent(_completed);
                }

                return _handle;
            }
        }

        public bool CompletedSynchronously => (SynchRead == Count);

        public bool IsCompleted
        {
            get
            {
                lock (_locker)
                {
                    return _completed;
                }
            }
        }
    }
}
