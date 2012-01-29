using System;
using System.Globalization;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    public class DefaultHubFactory : IHubFactory
    {
        private readonly IHubActivator _hubActivator;
        private readonly IHubTypeResolver _hubTypeResolver;

        public DefaultHubFactory(IHubActivator hubActivator, IHubTypeResolver hubTypeResolver)
        {
            if (hubActivator == null)
            {
                throw new ArgumentNullException("hubActivator");
            }

            if (hubTypeResolver == null)
            {
                throw new ArgumentNullException("hubTypeResolver");
            }

            _hubActivator = hubActivator;
            _hubTypeResolver = hubTypeResolver;
        }

        public IHub CreateHub(string hubName)
        {
            // Get the type name from the client
            Type type = _hubTypeResolver.ResolveType(hubName);

            if (type == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Unable to find '{0}'.", hubName));
            }

            if (!typeof(IHub).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "'{0}' does not implement {1}.", type.FullName, typeof(IHub).FullName));
            }

            return _hubActivator.Create(type);
        }
    }
}