using System;

namespace SignalR
{
    public static class GlobalHost
    {
        private static IDependencyResolver _resolver;
        private static readonly Lazy<IDependencyResolver> _defaultResolver = new Lazy<IDependencyResolver>(() => new DefaultDependencyResolver());

        /// <summary>
        /// Gets the the default <see cref="IDependencyResolver"/>
        /// </summary>
        public static IDependencyResolver DependencyResolver
        {
            get
            {
                return _resolver ?? _defaultResolver.Value;
            }
        }

        /// <summary>
        /// Gets the default <see cref="IConfigurationManager"/>
        /// </summary>
        public static IConfigurationManager Configuration
        {
            get
            {
                return DependencyResolver.Resolve<IConfigurationManager>();
            }
        }

        /// <summary>
        /// Gets the default <see cref="IConnectionManager"/>
        /// </summary>
        public static IConnectionManager ConnectionManager
        {
            get
            {
                return DependencyResolver.Resolve<IConnectionManager>();
            }
        }

        /// <summary>
        /// Sets the default dependency resolver.
        /// </summary>
        /// <param name="resolver">The new dependency resolver</param>
        public static void SetResolver(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }
    }
}
