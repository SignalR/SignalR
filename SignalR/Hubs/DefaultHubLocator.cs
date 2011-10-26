using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;

namespace SignalR.Hubs
{
    public class DefaultHubLocator : IHubLocator
    {
        public IEnumerable<Type> GetHubs()
        {
            return from Assembly a in BuildManager.GetReferencedAssemblies()
                   where !a.GlobalAssemblyCache && !a.IsDynamic
                   from type in GetTypesSafe(a)
                   where typeof(IHub).IsAssignableFrom(type) && !type.IsAbstract
                   select type;
        }

        private IEnumerable<Type> GetTypesSafe(Assembly a)
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