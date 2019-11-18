using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gottscj.Owin.Host.SocketHttpListener;
using Gottscj.Owin.Host.SocketHttpListener.Net.WebSockets;

namespace Gottscj.Owin.Host
{
    public class WebSocketManager
    {
        private readonly Dictionary<string, Func<WebSocketHandler>> _handlers = new Dictionary<string, Func<WebSocketHandler>>();
        private readonly Dictionary<string, WebSocketHandler> _sessions = new Dictionary<string, WebSocketHandler>();
        
        private readonly object _syncRoot = new object();
        
        internal void Add(string path, Func<WebSocketHandler> factory)
        {
            lock (_syncRoot)
            {
                _handlers[path] = factory;
            }
        }

        internal bool PathConfigured(string path)
        {
            lock (_syncRoot)
            {
                return _handlers.ContainsKey(path);
            }
        }

        public IReadOnlyList<WebSocketHandler> GetActiveSessions()
        {
            lock (_syncRoot)
            {
                return _sessions.Values.ToList();
            }
        }
        
        internal Task ProcessMessagesAsync(string path, WebSocketContext webSocketContext)
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

        private void RemoveFromSessions(string connectionId)
        {
            lock (_syncRoot)
            {
                _sessions.Remove(connectionId);
            }
        }
    }
}