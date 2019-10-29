using System;
using SocketHttpListener;

namespace WebSocketSharp.Owin.Sample
{
    public class ChatWebSocketHandler : WebSocketHandler
    {
        public ChatWebSocketHandler()
        {
            
        }

        protected override void OnConnected()
        {
            Console.WriteLine($"'{ConnectionId}' OnConnected");
        }

        protected override void OnTextMessage(string data)
        {
            Console.WriteLine($"'{ConnectionId}' OnTextMessage: {data}");
            Context.WebSocket.SendAsync(data, success => Console.WriteLine("SendResult: " + success));
        }

        protected override void OnBinaryMessage(byte[] data)
        {
            throw new NotImplementedException();
        }

        protected override void OnClose(ushort code, string reason, bool wasClean)
        {
            Console.WriteLine($"'{ConnectionId}' OnClose, {code} - {reason}");
        }

        protected override void OnError(string errorMessage)
        {
            Console.WriteLine($"'{ConnectionId}' OnError, {errorMessage}");
        }
    }
}