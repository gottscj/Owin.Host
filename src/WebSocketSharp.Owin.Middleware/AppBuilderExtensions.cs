using System;
using Microsoft.Owin.Builder;
using Owin;
using WebSocketSharp.Owin.WebSocketSharp;
using WebSocketSharp.Owin.WebSocketSharp.Server;

namespace WebSocketSharp.Owin.Middleware
{
    public static class AppBuilderExtensions
    {
        internal static readonly WebSocketServiceManager WebSocketServiceManager 
            = new WebSocketServiceManager(new Logger());
        public static IAppBuilder UseWebSocketSharp(this IAppBuilder app)
        {
            app.Use(typeof(WebSocketSharpMiddleware));
            return app;
        }

        public static IAppBuilder AddWebSocketHandler<THandler>(this IAppBuilder app, string path)
            where THandler : WebSocketHandler
        {
            WebSocketServiceManager.Add(path, Activator.CreateInstance<THandler>);
            return app;
        }
    }
}