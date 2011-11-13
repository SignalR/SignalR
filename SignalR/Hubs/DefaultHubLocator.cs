using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;

namespace SignalR.Hubs
{
    public class DefaultHubLocator : IHubLocator
    {
        private readonly Lazy<IEnumerable<Type>> _hubs = new Lazy<IEnumerable<Type>>(GetAllHubs);

        public IEnumerable<Type> GetHubs()
        {
            return _hubs.Value;
        }

        public static IEnumerable<Type> GetAllHubs()
        {
            return from Assembly a in BuildManager.GetReferencedAssemblies()
                   where !a.GlobalAssemblyCache && !a.IsDynamic
                   from type in GetTypesSafe(a)
                   where typeof(IHub).IsAssignableFrom(type) && !type.IsAbstract
                   select type;
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