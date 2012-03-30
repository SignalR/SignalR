using System.Collections.Generic;
using System.Reflection;

namespace SignalR.Hubs
{
    public interface IAssemblyLocator
    {
        IEnumerable<Assembly> GetAssemblies();
    }
}