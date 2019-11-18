using System.Security.Principal;

namespace Gottscj.Owin.Host.SocketHttpListener.Net
{
    public class HttpListenerBasicIdentity : GenericIdentity
    {
        private readonly string _password;

        public HttpListenerBasicIdentity(string username, string password)
            : base(username, "Basic")
        {
            _password = password;
        }

        public virtual string Password => _password;
    }
}
