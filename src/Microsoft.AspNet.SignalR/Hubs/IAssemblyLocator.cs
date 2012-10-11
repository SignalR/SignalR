using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public interface IAssemblyLocator
    {
        IEnumerable<Assembly> GetAssemblies();
    }
}