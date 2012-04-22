using System;

namespace SignalR
{
    public static class GlobalHost
    {
        private static IDependencyResolver _resolver;
        private static readonly Lazy<IDependencyResolver> _defaultResolver = new Lazy<IDependencyResolver>(() => new DefaultDependencyResolver());

        /// <summary>
        /// Current dependency resolver.
        /// </summary>
        public static IDependencyResolver DependencyResolver
        {
            get
            {
                return _resolver ?? _defaultResolver.Value;
            }
        }

        /// <summary>
        /// Provides access to server configuration.
        /// </summary>
        public static IConfigurationManager Configuration
        {
            get
            {
                return DependencyResolver.Resolve<IConfigurationManager>();
            }
        }

        /// <summary>
        /// Returns the connection manager.
        /// </summary>
        public static IConnectionManager ConnectionManager
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
