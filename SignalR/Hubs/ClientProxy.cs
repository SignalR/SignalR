using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class ClientProxy : DynamicObject, IClientProxy
    {
        private readonly Func<string, ClientHubInvocation, IEnumerable<string>, Task> _send;
        private readonly string _hubName;
        private readonly string[] _exclude;

        public ClientProxy(Func<string, ClientHubInvocation, IEnumerable<string>, Task> send, string hubName, params string[] exclude)
        {
            _send = send;
            _hubName = hubName;
            _exclude = exclude;
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

            return _send(_hubName, invocation, _exclude);
        }
    }
}
