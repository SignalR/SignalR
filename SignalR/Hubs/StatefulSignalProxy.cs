using System;
using System.Dynamic;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class StatefulSignalProxy : SignalProxy
    {
        private readonly TrackingDictionary _state;

        public StatefulSignalProxy(Func<string, ClientHubInvocation, Task> send, string signal, string hubName, TrackingDictionary state)
            : base(send, signal, hubName)
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

        protected override ClientHubInvocation GetInvocationData(string method, object[] args)
        {
            return new ClientHubInvocation
            {
                Hub = _hubName,
                Method = method,
                Args = args,
                Target = _signal,
                State = _state.GetChanges()
            };
        }
    }
}
