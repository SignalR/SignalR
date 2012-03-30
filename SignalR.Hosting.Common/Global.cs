using System;

namespace SignalR
{
    public static class Global
    {
        private static IDependencyResolver _resolver;
        private static readonly Lazy<IDependencyResolver> _defaultResolver = new Lazy<IDependencyResolver>(() => new DefaultDependencyResolver());

        public static IDependencyResolver DependencyResolver
        {
            get
            {
                return _resolver ?? _defaultResolver.Value;
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

        public static void SetResolver(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }
    }
}
