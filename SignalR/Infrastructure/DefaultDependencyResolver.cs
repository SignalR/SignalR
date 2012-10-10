using System;
using System.Collections.Generic;
using System.Linq;
using SignalR.Hubs;
using SignalR.Infrastructure;
using SignalR.Transports;

namespace SignalR
{
    public class DefaultDependencyResolver : IDependencyResolver
    {
        private readonly Dictionary<Type, IList<Func<object>>> _resolvers = new Dictionary<Type, IList<Func<object>>>();

        public DefaultDependencyResolver()
        {
            RegisterDefaultServices();

            // Hubs
            RegisterHubExtensions();
        }

        private void RegisterDefaultServices()
        {
            var traceManager = new Lazy<TraceManager>(() => new TraceManager());
            Register(typeof(ITraceManager), () => traceManager.Value);

            var serverIdManager = new ServerIdManager();
            Register(typeof(IServerIdManager), () => serverIdManager);

            var serverMessageHandler = new Lazy<IServerCommandHandler>(() => new ServerCommandHandler(this));
            Register(typeof(IServerCommandHandler), () => serverMessageHandler.Value);

            var newMessageBus = new Lazy<IMessageBus>(() => new MessageBus(this));
            Register(typeof(IMessageBus), () => newMessageBus.Value);

            var serializer = new Lazy<JsonNetSerializer>();
            Register(typeof(IJsonSerializer), () => serializer.Value);

            var connectionIdPrefixGenerator = new EmptyConnectionIdPrefixGenerator();
            Register(typeof(IConnectionIdPrefixGenerator), () => connectionIdPrefixGenerator);

            var transportManager = new Lazy<TransportManager>(() => new TransportManager(this));
            Register(typeof(ITransportManager), () => transportManager.Value);

            var configurationManager = new DefaultConfigurationManager();
            Register(typeof(IConfigurationManager), () => configurationManager);

            var transportHeartbeat = new Lazy<TransportHeartBeat>(() => new TransportHeartBeat(this));
            Register(typeof(ITransportHeartBeat), () => transportHeartbeat.Value);

            var connectionManager = new Lazy<ConnectionManager>(() => new ConnectionManager(this));
            Register(typeof(IConnectionManager), () => connectionManager.Value);

            var ackHandler = new Lazy<AckHandler>();
            Register(typeof(IAckHandler), () => ackHandler.Value);

            var perfCounterWriter = new Lazy<PerformanceCounterManager>(() => new PerformanceCounterManager());
            Register(typeof(IPerformanceCounterManager), () => perfCounterWriter.Value);
        }

        private void RegisterHubExtensions()
        {
            var methodDescriptorProvider = new Lazy<ReflectedMethodDescriptorProvider>();
            Register(typeof(IMethodDescriptorProvider), () => methodDescriptorProvider.Value);

            var hubDescriptorProvider = new Lazy<ReflectedHubDescriptorProvider>(() => new ReflectedHubDescriptorProvider(this));
            Register(typeof(IHubDescriptorProvider), () => hubDescriptorProvider.Value);

            var parameterBinder = new Lazy<DefaultParameterResolver>();
            Register(typeof(IParameterResolver), () => parameterBinder.Value);

            var activator = new Lazy<DefaultHubActivator>(() => new DefaultHubActivator(this));
            Register(typeof(IHubActivator), () => activator.Value);

            var hubManager = new Lazy<DefaultHubManager>(() => new DefaultHubManager(this));
            Register(typeof(IHubManager), () => hubManager.Value);

            var proxyGenerator = new Lazy<DefaultJavaScriptProxyGenerator>(() => new DefaultJavaScriptProxyGenerator(this));
            Register(typeof(IJavaScriptProxyGenerator), () => proxyGenerator.Value);

            var requestParser = new Lazy<HubRequestParser>();
            Register(typeof(IHubRequestParser), () => requestParser.Value);

            var assemblyLocator = new Lazy<DefaultAssemblyLocator>(() => new DefaultAssemblyLocator());
            Register(typeof(IAssemblyLocator), () => assemblyLocator.Value);

            // Setup the default hub pipeline
            var dispatcher = new Lazy<IHubPipeline>(() => new HubPipeline().AddModule(new AuthorizeModule()));
            Register(typeof(IHubPipeline), () => dispatcher.Value);
            Register(typeof(IHubPipelineInvoker), () => dispatcher.Value);
        }

        public virtual object GetService(Type serviceType)
        {
            IList<Func<object>> activators;
            if (_resolvers.TryGetValue(serviceType, out activators))
            {
                if (activators.Count == 0)
                {
                    return null;
                }
                if (activators.Count > 1)
                {
                    throw new InvalidOperationException(String.Format("Multiple activators for type {0} are registered. Please call GetServices instead.", serviceType.FullName));
                }
                return activators[0]();
            }
            return null;
        }

        public virtual IEnumerable<object> GetServices(Type serviceType)
        {
            IList<Func<object>> activators;
            if (_resolvers.TryGetValue(serviceType, out activators))
            {
                if (activators.Count == 0)
                {
                    return null;
                }
                return activators.Select(r => r()).ToList();
            }
            return null;
        }

        public virtual void Register(Type serviceType, Func<object> activator)
        {
            IList<Func<object>> activators;
            if (!_resolvers.TryGetValue(serviceType, out activators))
            {
                activators = new List<Func<object>>();
                _resolvers.Add(serviceType, activators);
            }
            else
            {
                activators.Clear();
            }
            activators.Add(activator);
        }

        public virtual void Register(Type serviceType, IEnumerable<Func<object>> activators)
        {
            IList<Func<object>> list;
            if (!_resolvers.TryGetValue(serviceType, out list))
            {
                list = new List<Func<object>>();
                _resolvers.Add(serviceType, list);
            }
            else
            {
                list.Clear();
            }
            foreach (var a in activators)
            {
                list.Add(a);
            }
        }
    }
}
