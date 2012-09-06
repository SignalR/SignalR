using System;
using System.Dynamic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class ClientProxy : DynamicObject, IClientProxy
    {
        private readonly Func<string, ClientHubInvocation, Task> _send;
        private readonly string _hubName;

        public ClientProxy(Func<string, ClientHubInvocation, Task> send, string hubName)
        {
            _send = send;
            _hubName = hubName;
        }

        public dynamic this[string key]
        {
            get
            {
                return new SignalProxy(_send, key, _hubName);
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this[binder.Name];
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = Invoke(binder.Name, args);
            return true;
        }

        public Task Invoke(string method, params object[] args)
        {
            var invocation = new ClientHubInvocation
            {
                Hub = _hubName,
                Method = method,
                Args = args
            };

            return _send(_hubName, invocation);
        }
    }
}
