using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    internal class ChunkedInputStream : RequestStream
    {
        private bool _disposed;
        private ChunkStream _decoder;
        private readonly HttpListenerContext _context;
        private bool _noMoreData;

        private class ReadBufferState
        {
            public readonly byte[] Buffer;
            public int Offset;
            public int Count;
            public int InitialCount;
            public readonly HttpStreamAsyncResult Ares;
            public ReadBufferState(byte[] buffer, int offset, int count,
                        HttpStreamAsyncResult ares)
            {
                Buffer = buffer;
                Offset = offset;
                Count = count;
                InitialCount = count;
                Ares = ares;
            }
        }

        public ChunkedInputStream(HttpListenerContext context, Stream stream,
                        byte[] buffer, int offset, int length)
            : base(stream, buffer, offset, length)
        {
            _context = context;
            WebHeaderCollection coll = (WebHeaderCollection)context.Request.Headers;
            _decoder = new ChunkStream(coll);
        }

        public ChunkStream Decoder
        {
            get => _decoder;
            set => _decoder = value;
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            IAsyncResult ares = BeginRead(buffer, offset, count, null, null);
            return EndRead(ares);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count,
                            AsyncCallback cback, object state)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            int len = buffer.Length;
            if (offset < 0 || offset > len)
                throw new ArgumentOutOfRangeException("offset exceeds the size of buffer");

            if (count < 0 || offset > len - count)
                throw new ArgumentOutOfRangeException("offset+size exceeds the size of buffer");

            HttpStreamAsyncResult ares = new HttpStreamAsyncResult();
            ares.Callback = cback;
            ares.State = state;
            if (_noMoreData)
            {
                ares.Complete();
                return ares;
            }
            int nread = _decoder.Read(buffer, offset, count);
            offset += nread;
            count -= nread;
            if (count == 0)
            {
                // got all we wanted, no need to bother the decoder yet
                ares.Count = nread;
                ares.Complete();
                return ares;
            }
            if (!_decoder.WantMore)
            {
                _noMoreData = nread == 0;
                ares.Count = nread;
                ares.Complete();
                return ares;
            }
            ares.Buffer = new byte[8192];
            ares.Offset = 0;
            ares.Count = 8192;
            ReadBufferState rb = new ReadBufferState(buffer, offset, count, ares);
            rb.InitialCount += nread;
            base.BeginRead(ares.Buffer, ares.Offset, ares.Count, OnRead, rb);
            return ares;
        }

        private void OnRead(IAsyncResult baseAres)
        {
            ReadBufferState rb = (ReadBufferState)baseAres.AsyncState;
            HttpStreamAsyncResult ares = rb.Ares;
            try
            {
                int nread = base.EndRead(baseAres);
                _decoder.Write(ares.Buffer, ares.Offset, nread);
                nread = _decoder.Read(rb.Buffer, rb.Offset, rb.Count);
                rb.Offset += nread;
                rb.Count -= nread;
                if (rb.Count == 0 || !_decoder.WantMore || nread == 0)
                {
                    _noMoreData = !_decoder.WantMore && nread == 0;
                    ares.Count = rb.InitialCount - rb.Count;
                    ares.Complete();
                    return;
                }
                ares.Offset = 0;
                ares.Count = Math.Min(8192, _decoder.ChunkLeft + 6);
                base.BeginRead(ares.Buffer, ares.Offset, ares.Count, OnRead, rb);
            }
            catch (Exception e)
            {
                _context.Connection.SendError(e.Message, 400);
                ares.Complete(e);
            }
        }

        public override int EndRead(IAsyncResult ares)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());

            HttpStreamAsyncResult myAres = ares as HttpStreamAsyncResult;
            if (ares == null)
                throw new ArgumentException("Invalid IAsyncResult", nameof(ares));

            if (!ares.IsCompleted)
                ares.AsyncWaitHandle.WaitOne();

            if (myAres.Error != null)
                throw new System.Net.HttpListenerException(400, "I/O operation aborted: " + myAres.Error.Message);

            return myAres.Count;
        }

        public override void Close()
        {
            if (!_disposed)
            {
                _disposed = true;
                base.Close();
            }
        }
    }
}
