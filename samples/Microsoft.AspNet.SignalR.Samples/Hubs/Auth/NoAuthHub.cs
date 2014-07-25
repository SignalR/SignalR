using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR.Hubs;


namespace Microsoft.AspNet.SignalR.Samples.Hubs.Auth
{
    public class NoAuthHub : Hub
    {
        public override Task OnDisconnected(bool stopCalled)
        {
            return Clients.All.left(Context.ConnectionId, DateTime.Now.ToString());
        }

        public override Task OnConnected()
        {
            return Clients.All.joined(Context.ConnectionId, DateTime.Now.ToString(), AuthInfo());
        }

        public override Task OnReconnected()
        {
            return Clients.All.rejoined(Context.ConnectionId, DateTime.Now.ToString());
        }

        public void InvokedFromClient()
        {
            Clients.All.invoked(Context.ConnectionId, DateTime.Now.ToString());
        }

        protected object AuthInfo()
        {
            var user = Context.User;
            return new
            {
                IsAuthenticated = user.Identity.IsAuthenticated,
                IsAdmin = user.IsInRole("Admin"),
                UserName = HttpUtility.HtmlEncode(user.Identity.Name)
            };
        }
    }
}