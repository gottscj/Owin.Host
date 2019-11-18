using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    internal class RequestStream : Stream
    {
        private readonly byte[] _buffer;
        private int _offset;
        private int _length;
        private long _remainingBody;
        private bool _disposed;
        private readonly Stream _stream;

        internal RequestStream(Stream stream, byte[] buffer, int offset, int length)
            : this(stream, buffer, offset, length, -1)
        {
        }

        internal RequestStream(Stream stream, byte[] buffer, int offset, int length, long contentlength)
        {
            _stream = stream;
            _buffer = buffer;
            _offset = offset;
            _length = length;
            _remainingBody = contentlength;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }


        public override void Close()
        {
            _disposed = true;
        }

        public override void Flush()
        {
        }


        // Returns 0 if we can keep reading from the base stream,
        // > 0 if we read something from the buffer.
        // -1 if we had a content length set and we finished reading that many bytes.
        private int FillFromBuffer(byte[] buffer, int off, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (off < 0)
                throw new ArgumentOutOfRangeException("offset", "< 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "< 0");
            int len = buffer.Length;
            if (off > len)
                throw new ArgumentException("destination offset is beyond array size");
            if (off > len - count)
                throw new ArgumentException("Reading would overrun buffer");

            if (_remainingBody == 0)
                return -1;

            if (_length == 0)
                return 0;

            int size = Math.Min(_length, count);
            if (_remainingBody > 0)
                size = (int)Math.Min(size, _remainingBody);

            if (_offset > _buffer.Length - size)
            {
                size = Math.Min(size, _buffer.Length - _offset);
            }
            if (size == 0)
                return 0;

            Buffer.BlockCopy(_buffer, _offset, buffer, off, size);
            _offset += size;
            _length -= size;
            if (_remainingBody > 0)
                _remainingBody -= size;
            return size;
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(RequestStream).ToString());

            // Call FillFromBuffer to check for buffer boundaries even when remaining_body is 0
            int nread = FillFromBuffer(buffer, offset, count);
            if (nread == -1)
            { // No more bytes available (Content-Length)
                return 0;
            }
            else if (nread > 0)
            {
                return nread;
            }

            nread = _stream.Read(buffer, offset, count);
            if (nread > 0 && _remainingBody > 0)
                _remainingBody -= nread;
            return nread;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count,
                            AsyncCallback cback, object state)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(RequestStream).ToString());

            int nread = FillFromBuffer(buffer, offset, count);
            if (nread > 0 || nread == -1)
            {
                HttpStreamAsyncResult ares = new HttpStreamAsyncResult();
                ares.Buffer = buffer;
                ares.Offset = offset;
                ares.Count = count;
                ares.Callback = cback;
                ares.State = state;
                ares.SynchRead = Math.Max(0, nread);
                ares.Complete();
                return ares;
            }

            // Avoid reading past the end of the request to allow
            // for HTTP pipelining
            if (_remainingBody >= 0 && count > _remainingBody)
                count = (int)Math.Min(Int32.MaxValue, _remainingBody);
            return _stream.BeginRead(buffer, offset, count, cback, state);
        }

        public override int EndRead(IAsyncResult ares)
        {
            if (_disposed)
                throw new ObjectDisposedException(typeof(RequestStream).ToString());

            if (ares == null)
                throw new ArgumentNullException("async_result");

            if (ares is HttpStreamAsyncResult)
            {
                HttpStreamAsyncResult r = (HttpStreamAsyncResult)ares;
                if (!ares.IsCompleted)
                    ares.AsyncWaitHandle.WaitOne();
                return r.SynchRead;
            }

            // Close on exception?
            int nread = _stream.EndRead(ares);
            if (_remainingBody > 0 && nread > 0)
                _remainingBody -= nread;
            return nread;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count,
                            AsyncCallback cback, object state)
        {
            throw new NotSupportedException();
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }
    }
}
