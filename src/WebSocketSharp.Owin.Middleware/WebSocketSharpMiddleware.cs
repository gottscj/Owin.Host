using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using WebSocketSharp.Owin.WebSocketSharp.Net;
using WebSocketSharp.Owin.WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Owin.WebSocketSharp.Server;
using HttpListenerContext = System.Net.HttpListenerContext;

namespace WebSocketSharp.Owin.Middleware
{
    public class WebSocketSharpMiddleware : OwinMiddleware
    {
        private readonly WebSocketServiceManager _services = AppBuilderExtensions.WebSocketServiceManager;
        
        public WebSocketSharpMiddleware(OwinMiddleware next) : base(next)
        {
            if (Type.GetType("Mono.Runtime") == null)
            {
                throw new InvalidOperationException("Only mono supported for now");
            }
            _services.Start();
        }

        public override async Task Invoke(IOwinContext context)
        {
            var httpListenerContext = context.Get<HttpListenerContext>("System.Net.HttpListenerContext");
            if (!Equals(context.Request.Headers["Upgrade"], "websocket"))
            {
                await Next.Invoke(context);
                return;
            }

            try
            {
                var webSocketContext = new SystemNetHttpListenerWebSocketContext(httpListenerContext, null);
                var path = context.Request.Path.Value;
                if (!_services.InternalTryGetServiceHost (path, out var host)) {
                    webSocketContext.Close (HttpStatusCode.NotImplemented);
                
                    await Next.Invoke(context);
                    return;
                }
                var tcs = new TaskCompletionSource<object>();
                host.StartSession (webSocketContext);
                webSocketContext.WebSocket.OnClose += (sender, args) => tcs.TrySetResult(null);
                await tcs.Task;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}