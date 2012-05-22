using System.Dynamic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class SignalAgent : DynamicObject
    {
        protected readonly IConnection _connection;
        protected readonly string _signal;
        protected readonly string _hubName;

        public SignalAgent(IConnection connection, string signal, string hubName)
        {
            _connection = connection;
            _signal = signal;
            _hubName = hubName;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            return false;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = Invoker(binder.Name, args);
            return true;
        }

        public Task Invoke(string method, params object[] args)
        {
            return Invoker(method, args);
        }

        private Task Invoker(string method, params object[] args)
        {
            var invocation = GetInvocationData(method, args);

            string signal = _hubName + "." + _signal;

            return _connection.Send(signal, invocation);
        }

        protected virtual object GetInvocationData(string method, object[] args)
        {
            return new
            {
                Hub = _hubName,
                Method = method,
                Args = args
            };
        }
    }
}
