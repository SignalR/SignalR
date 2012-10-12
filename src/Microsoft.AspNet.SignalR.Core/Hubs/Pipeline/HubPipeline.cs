﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class HubPipeline : IHubPipeline, IHubPipelineInvoker
    {
        private readonly Stack<IHubPipelineModule> _modules;
        private readonly Lazy<ComposedPipeline> _pipeline;

        public HubPipeline()
        {
            _modules = new Stack<IHubPipelineModule>();
            _pipeline = new Lazy<ComposedPipeline>(() => new ComposedPipeline(_modules));
        }

        public IHubPipeline AddModule(IHubPipelineModule builder)
        {
            if (_pipeline.IsValueCreated)
            {
                throw new InvalidOperationException("Unable to add module. The HubPipeline has already been invoked.");
            }
            _modules.Push(builder);
            return this;
        }

        private ComposedPipeline Pipeline
        {
            get { return _pipeline.Value; }
        }

        public Task<object> Invoke(IHubIncomingInvokerContext context)
        {
            return Pipeline.Invoke(context);
        }

        public Task Connect(IHub hub)
        {
            return Pipeline.Connect(hub);
        }

        public Task Reconnect(IHub hub)
        {
            return Pipeline.Reconnect(hub);
        }

        public Task Disconnect(IHub hub)
        {
            return Pipeline.Disconnect(hub);
        }

        public bool AuthorizeConnect(HubDescriptor hubDescriptor, IRequest request)
        {
            return Pipeline.AuthorizeConnect(hubDescriptor, request);
        }

        public IEnumerable<string> RejoiningGroups(HubDescriptor hubDescriptor, IRequest request, IEnumerable<string> groups)
        {
            return Pipeline.RejoiningGroups(hubDescriptor, request, groups);
        }

        public Task Send(IHubOutgoingInvokerContext context)
        {
            return Pipeline.Send(context);
        }

        private class ComposedPipeline
        {

            public Func<IHubIncomingInvokerContext, Task<object>> Invoke;
            public Func<IHub, Task> Connect;
            public Func<IHub, Task> Reconnect;
            public Func<IHub, Task> Disconnect;
            public Func<HubDescriptor, IRequest, bool> AuthorizeConnect;
            public Func<HubDescriptor, IRequest, IEnumerable<string>, IEnumerable<string>> RejoiningGroups;
            public Func<IHubOutgoingInvokerContext, Task> Send;

            public ComposedPipeline(Stack<IHubPipelineModule> modules)
            {
                // This wouldn't look nearly as gnarly if C# had better type inference, but now we don't need the ComposedModule or PassThroughModule.
                Invoke = Compose<Func<IHubIncomingInvokerContext, Task<object>>>(modules, (m, f) => m.BuildIncoming(f))(HubDispatcher.Incoming);
                Connect = Compose<Func<IHub, Task>>(modules, (m, f) => m.BuildConnect(f))(HubDispatcher.Connect);
                Reconnect = Compose<Func<IHub, Task>>(modules, (m, f) => m.BuildReconnect(f))(HubDispatcher.Reconnect);
                Disconnect = Compose<Func<IHub, Task>>(modules, (m, f) => m.BuildDisconnect(f))(HubDispatcher.Disconnect);
                AuthorizeConnect = Compose<Func<HubDescriptor, IRequest, bool>>(modules, (m, f) => m.BuildAuthorizeConnect(f))((h, r) => true);
                RejoiningGroups = Compose<Func<HubDescriptor, IRequest, IEnumerable<string>, IEnumerable<string>>>(modules, (m, f) => m.BuildRejoiningGroups(f))((h, r, g) => Enumerable.Empty<string>());
                Send = Compose<Func<IHubOutgoingInvokerContext, Task>>(modules, (m, f) => m.BuildOutgoing(f))(HubDispatcher.Outgoing);
            }

            // IHubPipelineModule could be turned into a second generic parameter, but it would make the above invocations even longer than they currently are.
            private Func<T, T> Compose<T>(IEnumerable<IHubPipelineModule> modules, Func<IHubPipelineModule, T, T> method)
            {
                // Notice we are reversing and aggregating in one step. (Function composition is associative) 
                return modules.Aggregate<IHubPipelineModule, Func<T, T>>(x => x, (a, b) => (x => method(b, a(x))));
            }
        }
    }
}
