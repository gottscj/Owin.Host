using System.Net;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    public delegate AuthenticationSchemes AuthenticationSchemeSelector(HttpListenerRequest httpRequest);
}
