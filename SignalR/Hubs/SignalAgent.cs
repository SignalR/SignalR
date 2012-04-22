using System.Dynamic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class SignalAgent : DynamicObject, IClientAgent
    {
        private readonly IConnection _connection;
        private readonly string _signal;
        private readonly string _hubName;
        private readonly TrackingDictionary _state;

        public SignalAgent(IConnection connection, string signal, string hubName)
            : this(connection, signal, hubName, null)
        {
        }

        public SignalAgent(IConnection connection, string signal, string hubName, TrackingDictionary state)
        {
            _connection = connection;
            _signal = signal;
            _hubName = hubName;
            _state = state;
        }

        public Task Invoke(string method, params object[] args)
        {
            return ClientAgent.Invoke(_connection, _signal, _hubName, method, args);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (_state == null)
            {
                return false;
            }

            _state[binder.Name] = value;
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_state == null)
            {
                result = null;
                return false;
            }

            result = _state[binder.Name];
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (_state != null)
            {
                result = InvokeWithState(binder.Name, args);
            }
            else
            {
                result = Invoke(binder.Name, args);
            }
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
