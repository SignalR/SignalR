// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public abstract class SignalProxy : DynamicObject, IClientProxy
    {
        private readonly IList<string> _exclude;

        protected SignalProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName, string prefix, IList<string> exclude)
        {
            Connection = connection;
            Invoker = invoker;
            HubName = hubName;
            Signal = prefix + hubName + "." + signal;
            _exclude = exclude;
        }

        protected IConnection Connection { get; private set; }
        protected IHubPipelineInvoker Invoker { get; private set; }
        protected string Signal { get; private set; }
        protected string HubName { get; private set; }

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

            var context = new HubOutgoingInvokerContext(Connection, Signal, invocation)
            {
                ExcludedSignals = _exclude
            };

            return Invoker.Send(context);
        }

        protected virtual ClientHubInvocation GetInvocationData(string method, object[] args)
        {
            return new ClientHubInvocation
            {
                Hub = HubName,
                Method = method,
                Args = args
            };
        }
    }
}
