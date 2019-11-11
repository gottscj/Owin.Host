using System;
using SocketHttpListener.Net.WebSockets;

namespace SocketHttpListener
{
    public abstract class WebSocketHandler
    {
        private static int _idCounter = 1;
        private WebSocketContext _context;

        protected WebSocketHandler()
        {
            lock (this)
            {
                ConnectionId = _idCounter.ToString();
                _idCounter = _idCounter + 1;
                if (_idCounter == int.MaxValue)
                {
                    _idCounter = 1;
                }
            }
            
            
        }
        public string ConnectionId { get; }

        public WebSocketContext Context
        {
            get => _context;
            internal set
            {
                _context = value;
                _context.WebSocket.OnOpen += (s, e) => OnConnected();
                _context.WebSocket.OnMessage += (s, e) =>
                {
                    switch (e.Type)
                    {
                        case Opcode.Text: OnTextMessage(e.Data); return;
                        case Opcode.Binary: OnBinaryMessage(e.RawData); return;
                    }
                };
                _context.WebSocket.OnClose += (s, e) => OnClose(e.Code, e.Reason, e.WasClean);
                _context.WebSocket.OnError += (s, e) => OnError(e.Message);
            }
        }

        protected abstract void OnConnected();

        protected abstract void OnTextMessage(string data);

        protected abstract void OnBinaryMessage(byte[] data);

        protected abstract void OnClose(ushort code, string reason, bool wasClean);

        protected abstract void OnError(string errorMessage);
    }
}