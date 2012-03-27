namespace SignalR.Hosting.AspNet.Infrastructure
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Web.Compilation;

    using SignalR.Infrastructure;

    public class AspNetAssemblyLocator : DefaultAssemblyLocator
    {
        public override IEnumerable<Assembly> GetAssemblies()
        {
            return base.GetAssemblies()
                .Concat(BuildManager.GetReferencedAssemblies().Cast<Assembly>())
                .Distinct();
        }
    }
}