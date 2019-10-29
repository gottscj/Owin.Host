using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocketHttpListener;
using SocketHttpListener.Net.WebSockets;

namespace WebSocketSharp.Owin
{
    internal class WebSocketManager
    {
        internal static readonly WebSocketManager Instance = new WebSocketManager();
        
        private readonly Dictionary<string, Func<WebSocketHandler>> _handlers = new Dictionary<string, Func<WebSocketHandler>>();
        private readonly Dictionary<int, WebSocketHandler> _sessions = new Dictionary<int, WebSocketHandler>();
        
        private readonly object _syncRoot = new object();
        
        public void Add(string path, Func<WebSocketHandler> factory)
        {
            lock (_syncRoot)
            {
                _handlers[path] = factory;
            }
        }

        public bool PathConfigured(string path)
        {
            lock (_syncRoot)
            {
                return _handlers.ContainsKey(path);
            }
        }

        public Task ProcessMessagesAsync(string path, WebSocketContext webSocketContext)
        {
            lock (_syncRoot)
            {
                if (!_handlers.TryGetValue(path, out var factory)) return null;
                
                var instance = factory();
                instance.Context = webSocketContext;
                webSocketContext.WebSocket.ConnectAsServer();
                _sessions[instance.ConnectionId] = instance;
                
                var tcs = new TaskCompletionSource<object>();
                instance.Context.WebSocket.OnClose += (s, e) =>
                {
                    RemoveFromSessions(instance.ConnectionId);
                    tcs.TrySetResult(null);
                };
                    
                return tcs.Task;
            }
        }

        private void RemoveFromSessions(int connectionId)
        {
            lock (_syncRoot)
            {
                _sessions.Remove(connectionId);
            }
        }
    }
}