using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;
using SignalR.Hubs;

namespace SignalR.AspNet
{
    public class BuildManagerTypeLocator : DefaultHubLocator
    {
        protected override IEnumerable<Assembly> GetAssemblies()
        {
            return BuildManager.GetReferencedAssemblies().Cast<Assembly>();
        }
    }
}
