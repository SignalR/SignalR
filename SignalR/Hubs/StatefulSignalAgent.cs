using System.Dynamic;

namespace SignalR.Hubs
{
    public class StatefulSignalAgent : SignalAgent
    {
        private readonly TrackingDictionary _state;

        public StatefulSignalAgent(IConnection connection, string signal, string hubName, TrackingDictionary state)
            : base(connection, signal, hubName)
        {
            _state = state;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _state[binder.Name] = value;
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = _state[binder.Name];
            return true;
        }
        
        protected override object GetInvocationData(string method, object[] args)
        {
            return new
            {
                Hub = _hubName,
                Method = method,
                Args = args,
                State = _state.GetChanges()
            };
        }
    }
}
