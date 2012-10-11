using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Hosting.AspNet.Infrastructure
{
    public class AspNetAssemblyLocator : DefaultAssemblyLocator
    {
        public override IEnumerable<Assembly> GetAssemblies()
        {
            return BuildManager.GetReferencedAssemblies().Cast<Assembly>();
        }
    }
}