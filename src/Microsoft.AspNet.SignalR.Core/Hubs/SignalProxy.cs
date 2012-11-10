// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class SignalProxy : DynamicObject, IClientProxy
    {
        protected readonly Func<string, ClientHubInvocation, IEnumerable<string>, Task> _send;
        protected readonly string _signal;
        protected readonly string _hubName;
        private readonly string[] _exclude;

        public SignalProxy(Func<string, ClientHubInvocation, IEnumerable<string>, Task> send, string signal, string hubName, params string[] exclude)
        {
            _send = send;
            _signal = signal;
            _hubName = hubName;
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

            string signal = _hubName + "." + _signal;

            return _send(signal, invocation, _exclude);
        }

        protected virtual ClientHubInvocation GetInvocationData(string method, object[] args)
        {
            return new ClientHubInvocation
            {
                Hub = _hubName,
                Method = method,
                Args = args,
                Target = _signal
            };
        }
    }
}
