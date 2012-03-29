using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;
using SignalR.Hubs;

namespace SignalR.Hosting.AspNet.Infrastructure
{
    public class AspNetAssemblyLocator : DefaultAssemblyLocator
    {
        public override IEnumerable<Assembly> GetAssemblies()
        {
            return BuildManager.GetReferencedAssemblies().Cast<Assembly>();
        }
    }
}