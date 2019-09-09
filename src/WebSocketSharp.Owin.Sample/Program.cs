using System;
using System.Web.Http;
using Owin;

namespace WebSocketSharp.Owin.Sample
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            using (OwinHost.Start("http://localhost:5000", app =>
            {
                var config = new HttpConfiguration();
                config.MapHttpAttributeRoutes();

                app.UseWebApi(config);
            }))
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}