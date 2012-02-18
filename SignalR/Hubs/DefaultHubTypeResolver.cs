using System;
using System.Collections.Generic;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class DefaultHubTypeResolver : IHubTypeResolver
    {
        private readonly Dictionary<string, Type> _hubCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        public DefaultHubTypeResolver(IDependencyResolver resolver)
            : this(resolver.Resolve<IHubLocator>())
        {
        }

        public DefaultHubTypeResolver(IHubLocator hubLocator)
        {
            if (hubLocator == null)
            {
                throw new ArgumentNullException("hubLocator");
            }

            BuildCache(hubLocator);
        }

        private void BuildCache(IHubLocator hubLocator)
        {
            foreach (var hubType in hubLocator.GetHubs())
            {
                // Always cache by full name
                AddCacheKey(hubType.FullName, hubType);

                // If there's a hub name attribute then use it as an alternative name
                var hubName = ReflectionHelper.GetAttributeValue<HubNameAttribute, string>(hubType, a => a.HubName);

                // Don't add it if it's the same as the short name
                if (!String.Equals(hubName, hubType.Name, StringComparison.OrdinalIgnoreCase))
                {
                    AddCacheKey(hubName, hubType);
                }

                // Add an entry for the type's short name
                AddCacheKey(hubType.Name, hubType);
            }
        }

        private void AddCacheKey(string key, Type hubType)
        {
            // Null/empty key is valid so do nothing
            if (String.IsNullOrEmpty(key))
            {
                return;
            }

            // If we have an ambiguous key then make it null
            if (_hubCache.ContainsKey(key))
            {
                _hubCache[key] = null;
            }
            else
            {
                // Otherwise it's a valid entry
                _hubCache[key] = hubType;
            }
        }

        public virtual Type ResolveType(string hubName)
        {
            Type type;
            if (_hubCache.TryGetValue(hubName, out type))
            {
                return type;
            }

            // Fallback to the build manager
            return null;
        }
    }
}
