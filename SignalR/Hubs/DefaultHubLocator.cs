using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SignalR.Hubs
{
    public class DefaultHubLocator : IHubLocator
    {
        private readonly Lazy<IEnumerable<Type>> _hubs;

        public DefaultHubLocator()
        {
            _hubs = new Lazy<IEnumerable<Type>>(GetAllHubs);
        }

        public IEnumerable<Type> GetHubs()
        {
            return _hubs.Value;
        }

        public IEnumerable<Type> GetAllHubs()
        {
            return (from a in GetAssemblies()
                    where !a.GlobalAssemblyCache && !a.IsDynamic
                    from type in GetTypesSafe(a)
                    where typeof(IHub).IsAssignableFrom(type) && !type.IsAbstract
                    select type).ToList();
        }

        protected virtual IEnumerable<Assembly> GetAssemblies()
        {
            // TODO: Look for a better default, chances are the hubs aren't even loaded yet
            return AppDomain.CurrentDomain.GetAssemblies();
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