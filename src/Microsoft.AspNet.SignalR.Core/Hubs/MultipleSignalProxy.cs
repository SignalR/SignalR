// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class MultipleSignalProxy : DynamicObject, IClientProxy
    {
        private readonly IConnection _connection;
        private readonly IHubPipelineInvoker _invoker;
        private readonly IList<string> _exclude;
        private readonly TraceSource _trace;
        private readonly IList<string> _signals;
        private readonly string _hubName;

        public MultipleSignalProxy(IConnection connection, IHubPipelineInvoker invoker, IList<string> signals, string hubName, string prefix, IList<string> exclude)
            : this(connection, invoker, signals, hubName, prefix, exclude, TraceManager.Default)
        {
        }

        public MultipleSignalProxy(IConnection connection, IHubPipelineInvoker invoker, IList<string> signals, string hubName, string prefix, IList<string> exclude, ITraceManager traceManager)
        {
            _connection = connection;
            _invoker = invoker;
            _hubName = hubName;
            _signals = signals.Select(signal => prefix + _hubName + "." + signal).ToList();
            _exclude = exclude;
            _trace = traceManager[$"SignalR.{nameof(MultipleSignalProxy)}"];
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
            if (_trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                var excluded = "";
                if (_exclude != null && _exclude.Count > 0)
                {
                    excluded = string.Join(",", _exclude);
                }
                var signals = "";
                if(_signals != null && _signals.Count > 0)
                {
                    signals = string.Join(",", _signals);
                }
                _trace.TraceVerbose($"Invoking {_hubName}.{method} with {args.Length} arguments (destination: {signals}; excluded: {excluded})");
            }

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
