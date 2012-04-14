using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SignalR.Hubs
{
    public class ReflectedHubDescriptorProvider : IHubDescriptorProvider
    {
        private readonly Lazy<IDictionary<string, HubDescriptor>> _hubs;
        private readonly Lazy<IAssemblyLocator> _locator;

        public ReflectedHubDescriptorProvider(IDependencyResolver resolver)
        {
            _locator = new Lazy<IAssemblyLocator>(resolver.Resolve<IAssemblyLocator>);
            _hubs = new Lazy<IDictionary<string, HubDescriptor>>(BuildHubsCache);
        }

        public IList<HubDescriptor> GetHubs()
        {
            return _hubs.Value
                .Select(kv => kv.Value)
                .Distinct()
                .ToList();
        }

        public bool TryGetHub(string hubName, out HubDescriptor descriptor)
        {
            return _hubs.Value.TryGetValue(hubName, out descriptor);
        }

        protected IDictionary<string, HubDescriptor> BuildHubsCache()
        {
            // Getting all IHub-implementing types that apply
            var types = _locator.Value.GetAssemblies()
                .Where(a => !a.GlobalAssemblyCache && !a.IsDynamic)
                .SelectMany(GetTypesSafe)
                .Where(IsHubType);

            // Building cache entries for each descriptor
            // Each descriptor is stored in dictionary under a key
            // that is it's name or the name provided by an attribute
            var cacheEntries = types
                .Select(type => new HubDescriptor
                                {
                                    Name = type.GetHubName(),
                                    Type = type
                                })
                .ToDictionary(hub => hub.Name,
                              hub => hub,
                              StringComparer.OrdinalIgnoreCase);

            return cacheEntries;
        }

        private static bool IsHubType(Type type)
        {
            try
            {
                return typeof(IHub).IsAssignableFrom(type) &&
                       !type.IsAbstract &&
                       (type.Attributes.HasFlag(TypeAttributes.Public) || 
                        type.Attributes.HasFlag(TypeAttributes.NestedPublic));
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<Type> GetTypesSafe(Assembly a)
        {
            try
            {
                return a.GetTypes();
            }
            catch
            {
                return Enumerable.Empty<Type>();
            }
        }
    }
}