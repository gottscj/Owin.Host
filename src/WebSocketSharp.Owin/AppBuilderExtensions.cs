using System;
using Owin;
using SocketHttpListener;
using WebSocketSharp.Owin.Middleware;

namespace WebSocketSharp.Owin
{
    public static class AppBuilderExtensions
    {
        public static IAppBuilder UseWebSockets(this IAppBuilder appBuilder, WebSocketManager webSocketManager)
        {
            AddWebSocketManager(appBuilder, webSocketManager);
            appBuilder.Use(typeof(WebSocketSharpMiddleware), webSocketManager);
            return appBuilder;
        }
        public static IAppBuilder UseWebSockets(this IAppBuilder appBuilder)
        {
            var webSocketManager = GetOrAddWebSocketManager(appBuilder);
            appBuilder.Use(typeof(WebSocketSharpMiddleware), webSocketManager);
            return appBuilder;
        }
        public static IAppBuilder AddWebSocketHandler<T>(this IAppBuilder appBuilder, string path, Func<T> factory) 
            where T : WebSocketHandler
        {
            GetOrAddWebSocketManager(appBuilder).Add(path, factory);
            return appBuilder;
        }
        
        public static IAppBuilder AddWebSocketHandler<T>(this IAppBuilder appBuilder, string path) 
            where T : WebSocketHandler, new()
        {
            GetOrAddWebSocketManager(appBuilder).Add(path, Activator.CreateInstance<T>);
            return appBuilder;
        }

        private static WebSocketManager GetOrAddWebSocketManager(IAppBuilder appBuilder)
        {
            const string webSocketManagerKey = nameof(WebSocketManager);
            var webSocketManager = appBuilder.Properties.Get<WebSocketManager>(webSocketManagerKey) ??
                                   AddWebSocketManager(appBuilder);

            return webSocketManager;
        }

        private static WebSocketManager AddWebSocketManager(IAppBuilder appBuilder)
        {
            return AddWebSocketManager(appBuilder, new WebSocketManager());
        }
        private static WebSocketManager AddWebSocketManager(IAppBuilder appBuilder, WebSocketManager webSocketManager)
        {
            const string webSocketManagerKey = nameof(WebSocketManager);
            appBuilder.Properties.Add(webSocketManagerKey, webSocketManager);
            return webSocketManager;
        }
    }
}