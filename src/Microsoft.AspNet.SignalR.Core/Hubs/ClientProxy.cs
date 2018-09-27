// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class ClientProxy : DynamicObject, IClientProxy
    {
        private readonly IHubPipelineInvoker _invoker;
        private readonly IConnection _connection;
        private readonly string _hubName;
        private readonly string _signal;
        private readonly TraceSource _trace;
        private readonly IList<string> _exclude;

        public ClientProxy(IConnection connection, IHubPipelineInvoker invoker, string hubName, IList<string> exclude) : this(connection, invoker, hubName, exclude, TraceManager.Default)
        {
        }

        public ClientProxy(IConnection connection, IHubPipelineInvoker invoker, string hubName, IList<string> exclude, ITraceManager traceManager)
        {
            _connection = connection;
            _invoker = invoker;
            _hubName = hubName;
            _exclude = exclude;
            _signal = PrefixHelper.GetHubName(_hubName);
            _trace = traceManager[$"SignalR.{nameof(ClientProxy)}"];
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Binder is passed in by the DLR")]
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
                _trace.TraceVerbose($"Invoking {_hubName}.{method} with {args.Length} arguments (destination: {_signal}; excluded: {excluded})");
            }

            var invocation = new ClientHubInvocation
            {
                Hub = _hubName,
                Method = method,
                Args = args
            };

            var context = new HubOutgoingInvokerContext(_connection, _signal, invocation)
            {
                ExcludedSignals = _exclude
            };

            return _invoker.Send(context);
        }
    }
}
