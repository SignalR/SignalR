using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class ReflectedHubDescriptorProvider: IHubDescriptorProvider
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

            // Building a list of descriptors for each type
            var descriptors = types.Select(type => 
                    new HubDescriptor
                    {
                        Name = GetHubName(type),
                        Type = type
                    });

            // Building cache entries for each descriptor
            // Each descriptor is stored in dictionary under several keys for allowing quick lookup:
            // full type name, short type name and optionally a name given by attribute
            var cacheEntries = descriptors
                .SelectMany(desc => CacheKeysFor(desc.Type)
                .Select(key => new { Descriptor = desc, Key = key }))
                .ToDictionary(anon => anon.Key, 
                              anon => anon.Descriptor,
                              StringComparer.OrdinalIgnoreCase);

            return cacheEntries;
        }

        private static bool IsHubType(Type type)
        {
            try
            {
                return typeof(IHub).IsAssignableFrom(type) && !type.IsAbstract;
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<string> CacheKeysFor(Type type)
        {
            yield return type.FullName;
            yield return type.Name;

            var attributeName = GetHubName(type);

            if (!String.Equals(attributeName, type.Name, StringComparison.OrdinalIgnoreCase))
            {
                yield return attributeName;
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

        private static string GetHubName(Type type)
        {
            return ReflectionHelper.GetAttributeValue<HubNameAttribute, string>(type, attr => attr.HubName) 
                   ?? type.Name;
        }
    }
}