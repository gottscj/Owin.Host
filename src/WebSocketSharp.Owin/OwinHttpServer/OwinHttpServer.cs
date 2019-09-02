using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Hosting.Engine;
using Microsoft.Owin.Hosting.ServerFactory;
using Microsoft.Owin.Hosting.Services;
using Owin;

namespace Shure.Cwb.WebApi.Service.OwinHttpServer
{
	public class OwinHttpServer : IDisposable
	{
        private IDisposable _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="OwinHttpServer"/> class.
        /// </summary>
        protected OwinHttpServer()
        {
        }

        /// <summary>
        /// Create a new TestServer instance and configure the OWIN pipeline.
        /// </summary>
        /// <param name="startup">Startup function used to configure the OWIN pipeline.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        public static OwinHttpServer Create(Action<IAppBuilder> startup)
        {
            var server = new OwinHttpServer();
            server.Configure(startup);
            return server;
        }

        /// <summary>
        /// Create a new <see cref="TestServer"/> instance and configure the OWIN pipeline.
        /// </summary>
        /// <typeparam name="TStartup">Class containing a startup function used to configure the OWIN pipeline.</typeparam>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        public static OwinHttpServer Create<TStartup>()
        {
            var server = new OwinHttpServer();
            server.Configure<TStartup>();
            return server;
        }

        /// <summary>
        /// Configures the OWIN pipeline.
        /// </summary>
        /// <param name="startup">Startup function used to configure the OWIN pipeline.</param>
        protected void Configure(Action<IAppBuilder> startup)
        {
            Configure(startup, null);
        }

        /// <summary>
        /// Configures the OWIN pipeline.
        /// </summary>
        /// <typeparam name="TStartup">Class containing a startup function used to configure the OWIN pipeline.</typeparam>
        protected void Configure<TStartup>()
        {
            Configure<TStartup>(null);
        }

        /// <summary>
        /// Configures the OWIN pipeline.
        /// </summary>
        /// <param name="startup">Startup function used to configure the OWIN pipeline.</param>
        /// <param name="options">Settings to control the startup behavior of an OWIN application</param>
        protected void Configure(Action<IAppBuilder> startup, StartOptions options)
        {
            // Compare with WebApp.StartImplementation
            if (startup == null)
            {
                throw new ArgumentNullException("startup");
            }

            options = options ?? new StartOptions();
            if (string.IsNullOrWhiteSpace(options.AppStartup))
            {
                // Populate AppStartup for use in host.AppName
                options.AppStartup = startup.Method.ReflectedType.FullName;
            }

            var webSocketHttpServerFactory = new OwinHttpServerFactory();
            var services = ServicesFactory.Create();
            var engine = services.GetService<IHostingEngine>();
            var context = new StartContext(options);
            context.ServerFactory = new ServerFactoryAdapter(webSocketHttpServerFactory);
            context.Startup = startup;
            _started = engine.Start(context);
        }

        /// <summary>
        /// Configures the OWIN pipeline.
        /// </summary>
        /// <typeparam name="TStartup">Class containing a startup function used to configure the OWIN pipeline.</typeparam>
        /// <param name="options">Settings to control the startup behavior of an OWIN application.</param>
        protected void Configure<TStartup>(StartOptions options)
        {
            // Compare with WebApp.StartImplementation
            options = options ?? new StartOptions();
            options.AppStartup = typeof(TStartup).AssemblyQualifiedName;

            var webSocketHttpServerFactory = new OwinHttpServerFactory();
            var services = ServicesFactory.Create();
            var engine = services.GetService<IHostingEngine>();
            var context = new StartContext(options);
            context.ServerFactory = new ServerFactoryAdapter(webSocketHttpServerFactory);
            _started = engine.Start(context);
        }

        /// <summary>
        /// Disposes TestServer and OWIN pipeline.
        /// </summary>
        public void Dispose()
        {
	        _started.Dispose();
        }
    }
}