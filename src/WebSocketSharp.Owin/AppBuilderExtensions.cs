using System;
using Owin;
using SocketHttpListener;
using WebSocketSharp.Owin.Middleware;

namespace WebSocketSharp.Owin
{
    public static class AppBuilderExtensions
    {
        public static IAppBuilder UseWebSockets(this IAppBuilder appBuilder)
        {
            appBuilder.Use(typeof(WebSocketSharpMiddleware), WebSocketManager.Instance);
            return appBuilder;
        }
        public static IAppBuilder AddWebSocketHandler<T>(this IAppBuilder appBuilder, string path, Func<T> factory) 
            where T : WebSocketHandler
        {
            WebSocketManager.Instance.Add(path, factory);
            return appBuilder;
        }
        
        public static IAppBuilder AddWebSocketHandler<T>(this IAppBuilder appBuilder, string path) 
            where T : WebSocketHandler, new()
        {
            WebSocketManager.Instance.Add(path, Activator.CreateInstance<T>);
            return appBuilder;
        }
    }
}