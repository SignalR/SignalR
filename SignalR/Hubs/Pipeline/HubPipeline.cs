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
        private Func<IHub, Task> _reconnectPipeline;
        private Func<IHub, Task> _disconnectPipeline;
        private Func<IHub, IEnumerable<string>, IEnumerable<string>> _rejoiningGroupsPipeline;
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
                _rejoiningGroupsPipeline = module.BuildRejoiningGroups(HubDispatcher.RejoiningGroups);
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

        public Task Reconnect(IHub hub)
        {
            EnsurePipeline();

            return _reconnectPipeline.Invoke(hub);
        }

        public Task Disconnect(IHub hub)
        {
            EnsurePipeline();

            return _disconnectPipeline.Invoke(hub);
        }

        public IEnumerable<string> RejoiningGroups(IHub hub, IEnumerable<string> groups)
        {
            EnsurePipeline();

            return _rejoiningGroupsPipeline(hub, groups);
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

            public Func<IHub, Task> BuildReconnect(Func<IHub, Task> reconnect)
            {
                return reconnect;
            }

            public Func<IHub, IEnumerable<string>, IEnumerable<string>> BuildRejoiningGroups(Func<IHub, IEnumerable<string>, IEnumerable<string>> rejoiningGroups)
            {
                return rejoiningGroups;
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
                return _left.BuildIncoming(_right.BuildIncoming(callback));
            }

            public Func<IHub, Task> BuildConnect(Func<IHub, Task> callback)
            {
                return _left.BuildConnect(_right.BuildConnect(callback));
            }

            public Func<IHub, Task> BuildReconnect(Func<IHub, Task> callback)
            {
                return _left.BuildReconnect(_right.BuildReconnect(callback));
            }

            public Func<IHub, Task> BuildDisconnect(Func<IHub, Task> callback)
            {
                return _left.BuildDisconnect(_right.BuildDisconnect(callback));
            }

            public Func<IHub, IEnumerable<string>, IEnumerable<string>> BuildRejoiningGroups(Func<IHub, IEnumerable<string>, IEnumerable<string>> callback)
            {
                return _left.BuildRejoiningGroups(_right.BuildRejoiningGroups(callback));
            }

            public Func<IHubOutgoingInvokerContext, Task> BuildOutgoing(Func<IHubOutgoingInvokerContext, Task> send)
            {
                return _left.BuildOutgoing(_right.BuildOutgoing(send));
            }
        }
    }
}
