using System.Dynamic;
using System.Threading.Tasks;

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

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = InvokeWithState(binder.Name, args);
            return true;
        }

        private Task InvokeWithState(string method, object[] args)
        {
            string signal = _hubName + "." + _signal;

            var invocation = new
            {
                Hub = _hubName,
                Method = method,
                Args = args,
                State = _state.GetChanges()
            };

            return _connection.Send(signal, invocation);
        }
    }
}
