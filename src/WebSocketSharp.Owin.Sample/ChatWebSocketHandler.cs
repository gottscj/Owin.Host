using System;
using WebSocketSharp.Owin.WebSocketSharp;
using WebSocketSharp.Owin.WebSocketSharp.Server;

namespace WebSocketSharp.Owin.Sample
{
    public class ChatWebSocketHandler : WebSocketHandler
    {
        public ChatWebSocketHandler()
        {
            
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Sessions.Broadcast(e.Data);
        }
    }
}