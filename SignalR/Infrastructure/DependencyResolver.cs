using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using SignalR.Hubs;

namespace SignalR.Infrastructure
{
    public static class DependencyResolver
    {
        private static readonly IDependencyResolver _defaultResolver = new DefaultDependencyResolver();
        private static IDependencyResolver _resolver;

        internal static IDependencyResolver Current
        {
            get { return _resolver ?? _defaultResolver; }
        }

        public static void SetResolver(IDependencyResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            _resolver = new FallbackDependencyResolver(resolver, _defaultResolver);
        }

        public static T Resolve<T>()
        {
            return (T)Current.GetService(typeof(T));
        }

        public static object Resolve(Type type)
        {
            return Current.GetService(type);
        }

        public static void Register(Type type, Func<object> activator)
        {
            Current.Register(type, activator);
        }

        public static void Register(Type serviceType, IEnumerable<Func<object>> activators)
        {
            Current.Register(serviceType, activators);
        }

        private class FallbackDependencyResolver : IDependencyResolver
        {
            private readonly IDependencyResolver _resolver;
            private readonly IDependencyResolver _fallbackResolver;

            public FallbackDependencyResolver(IDependencyResolver resolver, IDependencyResolver fallbackResolver)
            {
                _resolver = resolver;
                _fallbackResolver = fallbackResolver;
            }

            public object GetService(Type serviceType)
            {
                return _resolver.GetService(serviceType) ?? _fallbackResolver.GetService(serviceType);
            }

            public IEnumerable<object> GetServices(Type serviceType)
            {
                return _resolver.GetServices(serviceType).Concat(_fallbackResolver.GetServices(serviceType));
            }

            public void Register(Type serviceType, Func<object> activator)
            {
                _resolver.Register(serviceType, activator);
            }

            public void Register(Type serviceType, IEnumerable<Func<object>> activators)
            {
                _resolver.Register(serviceType, activators);
            }
        }

        private class DefaultDependencyResolver : IDependencyResolver
        {
            private readonly Dictionary<Type, IList<Func<object>>> _resolvers = new Dictionary<Type, IList<Func<object>>>();

            internal DefaultDependencyResolver()
            {
                var store = new Lazy<InProcessMessageStore>(() => new InProcessMessageStore());

                Register(typeof(IMessageStore), () => store.Value);

                var serialzier = new JavaScriptSerializerAdapter(new JavaScriptSerializer
                {
                    MaxJsonLength = 30 * 1024 * 1024
                });

                Register(typeof(IJsonStringifier), () => serialzier);

                Register(typeof(IActionResolver), () => new DefaultActionResolver());
                Register(typeof(IHubActivator), () => new DefaultHubActivator());
                Register(typeof(IHubFactory), () => new DefaultHubFactory());

                var hubLocator = new DefaultHubLocator();
                Register(typeof(IHubLocator), () => hubLocator);

                var signalBus = new InProcessSignalBus();
                Register(typeof(ISignalBus), () => signalBus);

                var pesistentConnectionFactory = new DefaultPersistentConnectionFactory();
                Register(typeof(IPersistentConnectionFactory), () => pesistentConnectionFactory);

                var minifier = new NullJavaScriptMinifier();

                var proxyGenerator = new DefaultJavaScriptProxyGenerator(hubLocator, (IJavaScriptMinifier)GetService(typeof(IJavaScriptMinifier)) ?? minifier);
                Register(typeof(IJavaScriptProxyGenerator), () => proxyGenerator);

                var clientIdFactory = new GuidClientIdFactory();
                Register(typeof(IClientIdFactory), () => clientIdFactory);
            }

            public object GetService(Type serviceType)
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

            public IEnumerable<object> GetServices(Type serviceType)
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

            public void Register(Type serviceType, Func<object> activator)
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

            public void Register(Type serviceType, IEnumerable<Func<object>> activators)
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
}