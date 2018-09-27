// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public abstract class SignalProxy : DynamicObject, IClientProxy
    {
        private readonly IList<string> _exclude;
        private readonly TraceSource _trace;

        protected SignalProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName, string prefix, IList<string> exclude)
            : this(connection, invoker, signal, hubName, prefix, TraceManager.Default, exclude)
        {
        }

        protected SignalProxy(IConnection connection, IHubPipelineInvoker invoker, string signal, string hubName, string prefix, ITraceManager traceManager, IList<string> exclude)
        {
            Connection = connection;
            Invoker = invoker;
            HubName = hubName;
            Signal = prefix + hubName + "." + signal;
            _exclude = exclude;
            _trace = traceManager[$"SignalR.{GetType().Name}"];
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
            if (_trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                var excluded = "";
                if (_exclude != null && _exclude.Count > 0)
                {
                    excluded = string.Join(",", _exclude);
                }
                _trace.TraceVerbose($"Invoking {HubName}.{method} with {args.Length} arguments (destination: {Signal}; excluded: {excluded})");
            }

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
