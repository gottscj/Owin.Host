using System;
using Owin;
using WebSocketSharp.Owin.WebSocketSharp.Server;

namespace WebSocketSharp.Owin
{
    public static class AppBuilderExtensions
    {
        public static void AddWebSocketHandler<T>(this IAppBuilder appBuilder, string path, Func<T> factory) 
            where T : WebSocketHandler
        {
            if (!appBuilder.Properties.TryGetValue("WebSocketSharp.HttpServer", out var httpServer))
            {
                throw new InvalidOperationException("Could not get HttpServer for attaching websocket handler");
            }

            ((HttpServer) httpServer).AddWebSocketService<T>(path, factory);
        }
        
        public static void AddWebSocketHandler<T>(this IAppBuilder appBuilder, string path) 
            where T : WebSocketHandler, new()
        {
            var httpServer = appBuilder.Properties.Get<HttpServer>(Constants.HttpServerKey);
            if (httpServer == null)
            {
                Console.WriteLine($"Could not get HttpServer for attaching websocket handler, expected it with key, '{Constants.HttpServerKey}'");
                return;
//                throw new InvalidOperationException("Could not get HttpServer for attaching websocket handler");
            }

            httpServer.AddWebSocketService<T>(path);
        }
    }
}