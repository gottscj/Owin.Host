using System.Threading.Tasks;
using Gottscj.Owin.Host.SocketHttpListener.Net;
using Gottscj.Owin.Host.SocketHttpListener.Net.WebSockets;
using Microsoft.Owin;

namespace Gottscj.Owin.Host
{
    internal class WebSocketSharpMiddleware : OwinMiddleware
    {
        private readonly WebSocketManager _webSocketManager;

        public WebSocketSharpMiddleware(OwinMiddleware next, WebSocketManager webSocketManager) : base(next)
        {
            _webSocketManager = webSocketManager;
        }

        public override async Task Invoke(IOwinContext context)
        {
            var httpListenerContext = context.Get<HttpListenerContext>(typeof(HttpListenerContext).FullName);
            if (!Equals(context.Request.Headers["Upgrade"], "websocket"))
            {
                await Next.Invoke(context);
                return;
            }
            var path = context.Request.Path.Value;
            
            if (!_webSocketManager.PathConfigured(path))
            {
                await Next.Invoke(context);
                return;
            }
            
            var webSocketContext = new HttpListenerWebSocketContext(httpListenerContext, null);
            
            await _webSocketManager.ProcessMessagesAsync(path, webSocketContext);
        }
    }
}