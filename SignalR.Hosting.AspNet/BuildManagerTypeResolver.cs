using System;
using System.Web.Compilation;
using SignalR.Hubs;

namespace SignalR.Hosting.AspNet
{
    public class BuildManagerTypeResolver : DefaultHubTypeResolver
    {
        public override Type ResolveType(string hubName)
        {
            return base.ResolveType(hubName) ?? BuildManager.GetType(hubName, throwOnError: false);
        }
    }
}
