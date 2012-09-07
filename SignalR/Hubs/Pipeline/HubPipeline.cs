using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR.Hubs
{
    public class HubPipeline : IHubPipeline, IHubPipelineInvoker
    {
        private readonly Stack<IHubPipelineModule> _modules = new Stack<IHubPipelineModule>();

        private Func<IHubIncomingInvokerContext, Task<object>> _incomingPipeline;
        private Func<IHub, Task> _connectPipeline;
        private Func<IHub, IEnumerable<string>, Task> _reconnectPipeline;
        private Func<IHub, Task> _disconnectPipeline;
        private Func<IHubOutgoingInvokerContext, Task> _outgoingPipeling;

        public HubPipeline()
        {
            // Add one item to the list so we don't have to special case the logic if
            // there's no builders in the pipeline
            AddModule(new PassThroughModule());
        }

        public IHubPipeline AddModule(IHubPipelineModule builder)
        {
            _modules.Push(builder);
            return this;
        }

        private void EnsurePipeline()
        {
            if (_incomingPipeline == null)
            {
                IHubPipelineModule module = _modules.Reverse().Aggregate((a, b) => new ComposedModule(a, b));
                _incomingPipeline = module.BuildIncoming(HubDispatcher.Incoming);
                _connectPipeline = module.BuildConnect(HubDispatcher.Connect);
                _reconnectPipeline = module.BuildReconnect(HubDispatcher.Reconnect);
                _disconnectPipeline = module.BuildDisconnect(HubDispatcher.Disconnect);
                _outgoingPipeling = module.BuildOutgoing(HubDispatcher.Outgoing);
            }
        }

        public Task<object> Invoke(IHubIncomingInvokerContext context)
        {
            EnsurePipeline();

            return _incomingPipeline.Invoke(context);
        }

        public Task Connect(IHub hub)
        {
            EnsurePipeline();

            return _connectPipeline.Invoke(hub);
        }

        public Task Reconnect(IHub hub, IEnumerable<string> groups)
        {
            EnsurePipeline();

            return _reconnectPipeline.Invoke(hub, groups);
        }

        public Task Disconnect(IHub hub)
        {
            EnsurePipeline();

            return _disconnectPipeline.Invoke(hub);
        }

        public Task Send(IHubOutgoingInvokerContext context)
        {
            EnsurePipeline();

            return _outgoingPipeling.Invoke(context);
        }

        private class PassThroughModule : IHubPipelineModule
        {
            public Func<IHubIncomingInvokerContext, Task<object>> BuildIncoming(Func<IHubIncomingInvokerContext, Task<object>> invoke)
            {
                return invoke;
            }

            public Func<IHubOutgoingInvokerContext, Task> BuildOutgoing(Func<IHubOutgoingInvokerContext, Task> send)
            {
                return send;
            }

            public Func<IHub, Task> BuildConnect(Func<IHub, Task> connect)
            {
                return connect;
            }

            public Func<IHub, IEnumerable<string>, Task> BuildReconnect(Func<IHub, IEnumerable<string>, Task> reconnect)
            {
                return reconnect;
            }

            public Func<IHub, Task> BuildDisconnect(Func<IHub, Task> disconnect)
            {
                return disconnect;
            }
        }

        private class ComposedModule : IHubPipelineModule
        {
            private readonly IHubPipelineModule _left;
            private readonly IHubPipelineModule _right;

            public ComposedModule(IHubPipelineModule left, IHubPipelineModule right)
            {
                _left = left;
                _right = right;
            }

            public Func<IHubIncomingInvokerContext, Task<object>> BuildIncoming(Func<IHubIncomingInvokerContext, Task<object>> callback)
            {
                return context =>
                {
                    return _left.BuildIncoming(_right.BuildIncoming(callback))(context);
                };
            }

            public Func<IHub, Task> BuildConnect(Func<IHub, Task> callback)
            {
                return hub =>
                {
                    return _left.BuildConnect(_right.BuildConnect(callback))(hub);
                };
            }

            public Func<IHub, IEnumerable<string>, Task> BuildReconnect(Func<IHub, IEnumerable<string>, Task> callback)
            {
                return (hub, groups) =>
                {
                    return _left.BuildReconnect(_right.BuildReconnect(callback))(hub, groups);
                };
            }

            public Func<IHub, Task> BuildDisconnect(Func<IHub, Task> callback)
            {
                return hub =>
                {
                    return _left.BuildDisconnect(_right.BuildDisconnect(callback))(hub);
                };
            }

            public Func<IHubOutgoingInvokerContext, Task> BuildOutgoing(Func<IHubOutgoingInvokerContext, Task> send)
            {
                return context =>
                {
                    return _left.BuildOutgoing(_right.BuildOutgoing(send))(context);
                };
            }
        }
    }
}
