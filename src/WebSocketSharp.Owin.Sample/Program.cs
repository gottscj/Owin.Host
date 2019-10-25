using System;
using System.Web.Http;
using Microsoft.Owin.Hosting;
using Owin;
using WebSocketSharp.Owin.Middleware;

namespace WebSocketSharp.Owin.Sample
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            using (WebApp.Start("http://localhost:5000", app =>
            {
                var config = new HttpConfiguration();
                config.MapHttpAttributeRoutes();

                app.UseWebApi(config);
                app.UseWebSocketSharp();
                app.AddWebSocketHandler<ChatWebSocketHandler>("/chat");
            }))
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}