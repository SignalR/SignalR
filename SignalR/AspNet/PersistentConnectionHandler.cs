using System;
using System.Threading.Tasks;
using System.Web;
using SignalR.Web;

namespace SignalR.Aspnet
{
    internal class PersistentConnectionHandler : HttpTaskAsyncHandler
    {
        private readonly PersistentConnection _connection;

        public PersistentConnectionHandler(PersistentConnection persistentConnection)
        {
            _connection = persistentConnection;
        }

        public override Task ProcessRequestAsync(HttpContextBase context)
        {
            return _connection.ProcessRequestAsync(context);
        }
    }
}
