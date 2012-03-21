using System;

namespace SignalR
{
    public static class Global
    {
        private static readonly Lazy<IDependencyResolver> _resolver = new Lazy<IDependencyResolver>(() => new DefaultDependencyResolver());

        public static IDependencyResolver DependencyResolver
        {
            get
            {
                return _resolver.Value;
            }
        }

        public static IConfigurationManager Configuration
        {
            get
            {
                return DependencyResolver.Resolve<IConfigurationManager>();
            }
        }

        public static IConnectionManager Connections
        {
            get
            {
                return DependencyResolver.Resolve<IConnectionManager>();
            }
        }
    }
}
