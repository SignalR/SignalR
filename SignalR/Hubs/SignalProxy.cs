using System;
using System.Dynamic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class SignalProxy : DynamicObject, IClientProxy
    {
        protected readonly Func<string, ClientHubInvocation, Task> _send;
        protected readonly string _signal;
        protected readonly string _hubName;

        public SignalProxy(Func<string, ClientHubInvocation, Task> send, string signal, string hubName)
        {
            _send = send;
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
            result = Invoke(binder.Name, args);
            return true;
        }

        public Task Invoke(string method, params object[] args)
        {
            var invocation = GetInvocationData(method, args);

            string signal = _hubName + "." + _signal;

            return _send.Invoke(signal, invocation);
        }

        protected virtual ClientHubInvocation GetInvocationData(string method, object[] args)
        {
            return new ClientHubInvocation
            {
                Hub = _hubName,
                Method = method,
                Args = args,
                GroupOrConnectionId = _signal
            };
        }
    }
}
