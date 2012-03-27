namespace SignalR.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class DefaultAssemblyLocator : IAssemblyLocator
    {
        public virtual IEnumerable<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }
    }
}