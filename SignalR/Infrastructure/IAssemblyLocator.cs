namespace SignalR.Infrastructure
{
    using System.Collections.Generic;
    using System.Reflection;

    public interface IAssemblyLocator
    {
        IEnumerable<Assembly> GetAssemblies();
    }
}