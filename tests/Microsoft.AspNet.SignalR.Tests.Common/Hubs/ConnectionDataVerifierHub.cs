using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class ConnectionDataVerifierHub : Hub
    {
        public override Task OnConnected()
        {
            ValidConnectionData();

            return base.OnConnected();
        }

        public override Task OnReconnected()
        {
            ValidConnectionData();

            return base.OnReconnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            ValidConnectionData();

            return base.OnDisconnected(stopCalled);
        }

        private bool ValidConnectionData()
        {
            if (Context.Request.QueryString["connectionData"] == null || !Context.Request.QueryString["connectionData"].ToLower().Contains("connectiondataverifierhub"))
            {
                Clients.Caller.fail();

                return false;
            }

            return true;
        }

        public void Ping()
        {
            if (ValidConnectionData())
            {
                Clients.Caller.pong();
            }
        }
    }
}
