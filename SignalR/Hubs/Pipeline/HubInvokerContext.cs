namespace SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public class HubInvokerContext : IHubIncomingInvokerContext
    {
        public HubInvokerContext(IHub hub, TrackingDictionary state, MethodDescriptor methodDescriptor, object[] args)
        {
            Hub = hub;
            MethodDescriptor = methodDescriptor;
            Args = args;
            State = state;
        }

        public IHub Hub
        {
            get;
            private set;
        }

        public MethodDescriptor MethodDescriptor
        {
            get;
            private set;
        }

        public object[] Args
        {
            get;
            private set;
        }


        public TrackingDictionary State
        {
            get;
            private set;
        }
    }
}
