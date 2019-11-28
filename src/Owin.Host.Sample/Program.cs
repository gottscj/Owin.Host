using System;
using System.Web.Http;
using Gottscj.Owin.Host;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
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

                var physicalFileSystem = new PhysicalFileSystem(@"wwwroot");
                var options = new FileServerOptions
                {
                    EnableDefaultFiles = true,
                    FileSystem = physicalFileSystem,
                    StaticFileOptions = {FileSystem = physicalFileSystem, ServeUnknownFileTypes = true},
                    DefaultFilesOptions = {DefaultFileNames = new[] {"index.html"}}
                };
                
                app.UseWebApi(config);
                app.UseWebSockets()
                    .AddWebSocketHandler<ChatWebSocketHandler>("/chat");
                app.UseFileServer(options);
            }))
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}