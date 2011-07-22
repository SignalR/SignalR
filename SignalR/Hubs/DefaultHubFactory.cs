using System;
using System.Globalization;
using System.Web.Compilation;
using SignalR.Infrastructure;

namespace SignalR.Hubs {
    public class DefaultHubFactory : IHubFactory {
        private IHubActivator _hubActivator;

        public DefaultHubFactory()
            : this(DependencyResolver.Resolve<IHubActivator>()) {

        }

        public DefaultHubFactory(IHubActivator hubActivator) {
            if (hubActivator == null) {
                throw new ArgumentNullException("hubActivator");
            }
            _hubActivator = hubActivator;
        }

        public Hub CreateHub(string hubName) {
            // Get the type name from the client
            Type type = BuildManager.GetType(hubName, throwOnError: false);

            if (type == null) {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "Unable to find '{0}'.", hubName));
            }

            if (!type.IsSubclassOf(typeof(Hub))) {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, "'{0}' is not a Hub.", type.FullName));
            }

            return _hubActivator.Create(type);
        }
    }
}