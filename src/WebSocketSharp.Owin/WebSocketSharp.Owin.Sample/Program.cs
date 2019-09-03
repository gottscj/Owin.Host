using Microsoft.Owin.Hosting;

namespace WebSocketSharp.Owin.Sample
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var options = new StartOptions
            {
                Urls = {"http://localhost:5000"}
            };
            using (var server = OwinHttpServer.Create(opt =>
            {
                opt.
                
            }, options))
        }
    }
}