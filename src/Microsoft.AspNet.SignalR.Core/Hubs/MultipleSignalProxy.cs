// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class MultipleSignalProxy : DynamicObject, IClientProxy
    {
        private readonly IConnection _connection;
        private readonly IHubPipelineInvoker _invoker;
        private readonly IList<string> _exclude;
        private readonly IList<string> _signals;
        private readonly string _hubName;

        public MultipleSignalProxy(IConnection connection, IHubPipelineInvoker invoker, IList<string> signals, string hubName, string prefix, IList<string> exclude)
        {
            _connection = connection;
            _invoker = invoker;
            _hubName = hubName;
            _signals = signals.Select(signal => prefix + _hubName + "." + signal).ToList();
            _exclude = exclude;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            return false;
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "The compiler generates calls to invoke this")]
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = Invoke(binder.Name, args);
            return true;
        }

        public Task Invoke(string method, params object[] args)
        {
            var invocation = GetInvocationData(method, args);

            var context = new HubOutgoingInvokerContext(_connection, _signals, invocation)
            {
                ExcludedSignals = _exclude
            };

            return _invoker.Send(context);
        }

        protected virtual ClientHubInvocation GetInvocationData(string method, object[] args)
        {
            return new ClientHubInvocation
            {
                Hub = _hubName,
                Method = method,
                Args = args
            };
        }
    }
}
