// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class HubPipeline : IHubPipeline, IHubPipelineInvoker
    {
        private readonly Stack<IHubPipelineModule> _modules;
        private readonly Lazy<ComposedPipeline> _pipeline;
        private readonly TraceSource _trace;

        public HubPipeline(IDependencyResolver resolver)
        {
            _modules = new Stack<IHubPipelineModule>();
            _pipeline = new Lazy<ComposedPipeline>(() => new ComposedPipeline(_modules));
            _trace = resolver.Resolve<ITraceManager>()[$"SignalR.{nameof(HubPipeline)}"];
        }

        public IHubPipeline AddModule(IHubPipelineModule pipelineModule)
        {
            if (_pipeline.IsValueCreated)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_UnableToAddModulePiplineAlreadyInvoked));
            }
            _trace.TraceInformation($"Adding pipeline module {pipelineModule.GetType().FullName}");
            _modules.Push(pipelineModule);
            return this;
        }

        private ComposedPipeline Pipeline
        {
            get { return _pipeline.Value; }
        }

        public Task<object> Invoke(IHubIncomingInvokerContext context)
        {
            _trace.TraceInformation($"Starting invoke pipeline for invocation of {context.MethodDescriptor.Hub.Name}.{context.MethodDescriptor.Name}");
            return Pipeline.Invoke(context);
        }

        public Task Connect(IHub hub)
        {
            _trace.TraceInformation($"Starting connect pipeline for {hub.GetType().FullName}");
            return Pipeline.Connect(hub);
        }

        public Task Reconnect(IHub hub)
        {
            _trace.TraceInformation($"Starting reconnect pipeline for {hub.GetType().FullName}");
            return Pipeline.Reconnect(hub);
        }

        public Task Disconnect(IHub hub, bool stopCalled)
        {
            _trace.TraceInformation($"Starting disconnect pipeline for {hub.GetType().FullName} (stopCalled: {stopCalled})");
            return Pipeline.Disconnect(hub, stopCalled);
        }

        public bool AuthorizeConnect(HubDescriptor hubDescriptor, IRequest request)
        {
            _trace.TraceInformation($"Starting authorize pipeline for {hubDescriptor.Name}");
            return Pipeline.AuthorizeConnect(hubDescriptor, request);
        }

        public IList<string> RejoiningGroups(HubDescriptor hubDescriptor, IRequest request, IList<string> groups)
        {
            _trace.TraceInformation($"Starting rejoining groups pipeline for {hubDescriptor.Name}");
            return Pipeline.RejoiningGroups(hubDescriptor, request, groups);
        }

        public Task Send(IHubOutgoingInvokerContext context)
        {
            _trace.TraceInformation($"Starting send pipeline for {context.Invocation.Hub}.{context.Invocation.Method}");
            return Pipeline.Send(context);
        }

        private class ComposedPipeline
        {
            public Func<IHubIncomingInvokerContext, Task<object>> Invoke;
            public Func<IHub, Task> Connect;
            public Func<IHub, Task> Reconnect;
            public Func<IHub, bool, Task> Disconnect;
            public Func<HubDescriptor, IRequest, bool> AuthorizeConnect;
            public Func<HubDescriptor, IRequest, IList<string>, IList<string>> RejoiningGroups;
            public Func<IHubOutgoingInvokerContext, Task> Send;

            public ComposedPipeline(Stack<IHubPipelineModule> modules)
            {
                // This wouldn't look nearly as gnarly if C# had better type inference, but now we don't need the ComposedModule or PassThroughModule.
                Invoke = Compose<Func<IHubIncomingInvokerContext, Task<object>>>(modules, (m, f) => m.BuildIncoming(f))(HubDispatcher.Incoming);
                Connect = Compose<Func<IHub, Task>>(modules, (m, f) => m.BuildConnect(f))(HubDispatcher.Connect);
                Reconnect = Compose<Func<IHub, Task>>(modules, (m, f) => m.BuildReconnect(f))(HubDispatcher.Reconnect);
                Disconnect = Compose<Func<IHub, bool, Task>>(modules, (m, f) => m.BuildDisconnect(f))(HubDispatcher.Disconnect);
                AuthorizeConnect = Compose<Func<HubDescriptor, IRequest, bool>>(modules, (m, f) => m.BuildAuthorizeConnect(f))((h, r) => true);
                RejoiningGroups = Compose<Func<HubDescriptor, IRequest, IList<string>, IList<string>>>(modules, (m, f) => m.BuildRejoiningGroups(f))((h, r, g) => g);
                Send = Compose<Func<IHubOutgoingInvokerContext, Task>>(modules, (m, f) => m.BuildOutgoing(f))(HubDispatcher.Outgoing);
            }

            // IHubPipelineModule could be turned into a second generic parameter, but it would make the above invocations even longer than they currently are.
            private static Func<T, T> Compose<T>(IEnumerable<IHubPipelineModule> modules, Func<IHubPipelineModule, T, T> method)
            {
                // Notice we are reversing and aggregating in one step. (Function composition is associative) 
                return modules.Aggregate<IHubPipelineModule, Func<T, T>>(x => x, (a, b) => (x => method(b, a(x))));
            }
        }
    }
}
