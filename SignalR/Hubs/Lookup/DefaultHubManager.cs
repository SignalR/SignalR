using System;
using System.Collections.Generic;
using System.Linq;
using SignalR.Hubs.Lookup.Descriptors;

namespace SignalR.Hubs.Lookup
{
    public class DefaultHubManager : IHubManager
    {
        private readonly IEnumerable<IActionDescriptorProvider> _actionProviders;
        private readonly IHubActivator _activator;
        private readonly IEnumerable<IHubDescriptorProvider> _hubProviders;

        public DefaultHubManager(IDependencyResolver resolver)
        {
            _hubProviders = resolver.ResolveAll<IHubDescriptorProvider>();
            _actionProviders = resolver.ResolveAll<IActionDescriptorProvider>();
            _activator = resolver.Resolve<IHubActivator>();
        }

        public HubDescriptor GetHub(string hubName)
        {
            HubDescriptor descriptor = null;
            if(_hubProviders.Single(p => p.TryGetHub(hubName, out descriptor)) != null)
            {
                return descriptor;
            }

            return null;
        }

        public IEnumerable<HubDescriptor> GetHubs(Predicate<HubDescriptor> predicate = null)
        {
            return _hubProviders
                .SelectMany(p => p.GetHubs())
                .Where(d => predicate == null || predicate(d));
        }

        public ActionDescriptor GetHubAction(string hubName, string actionName, params object[] parameters)
        {
            var hub = GetHub(hubName);

            if (hub == null)
            {
                return null;
            }

            ActionDescriptor descriptor = null;
            if (_actionProviders.Count(p => p.TryGetAction(hub, actionName, out descriptor, parameters)) == 1)
            {
                return descriptor;
            }

            return null;
        }

        public IEnumerable<ActionDescriptor> GetHubActions(string hubName, Predicate<ActionDescriptor> predicate = null)
        {
            var hub = GetHub(hubName);

            if (hub == null)
            {
                return null;
            }

            return _actionProviders
                    .SelectMany(p => p.GetActions(hub))
                    .Where(d => predicate == null || predicate(d));
        }

        public IHub ResolveHub(string hubName)
        {
            var hub = GetHub(hubName);
            return hub == null ? null : _activator.Create(hub);
        }

        public IEnumerable<IHub> ResolveHubs()
        {
            return GetHubs().Select(d => _activator.Create(d));
        }
    }
}
