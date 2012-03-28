using System.Collections.Generic;
using System.Reflection;

namespace SignalR.Infrastructure
{
    public interface IAssemblyLocator
    {
        IEnumerable<Assembly> GetAssemblies();
    }
}