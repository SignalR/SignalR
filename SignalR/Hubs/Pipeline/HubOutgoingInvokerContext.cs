namespace SignalR.Hubs
{
    public class HubOutgoingInvokerContext : IHubOutgoingInvokerContext
    {        
        public HubOutgoingInvokerContext(IConnection connection, string signal, ClientHubInvocation invocation)
        {
            Connection = connection;
            Signal = signal;
            Invocation = invocation;
        }

        public IConnection Connection
        {
            get;
            private set;
        }

        public string Group
        {
            get;
            private set;
        }

        public ClientHubInvocation Invocation
        {
            get;
            private set;
        }

        public string Signal
        {
            get;
            private set;
        }
    }
}
