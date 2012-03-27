namespace SignalR.Hubs.Lookup
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SignalR.Hubs.Lookup.Descriptors;

    public class DefaultHubManager : IHubManager
    {
        #region Constants and Fields

        private readonly IEnumerable<IActionDescriptorProvider> _actionProviders;
        private readonly IHubActivator _activator;

        private readonly IEnumerable<IHubDescriptorProvider> _hubProviders;

        #endregion

        #region Constructors and Destructors

        public DefaultHubManager(IDependencyResolver resolver)
        {
            this._hubProviders = resolver.ResolveAll<IHubDescriptorProvider>();
            this._actionProviders = resolver.ResolveAll<IActionDescriptorProvider>();
            this._activator = resolver.Resolve<IHubActivator>();
        }

        #endregion

        #region Public Methods and Operators

        public HubDescriptor GetHub(string hubName)
        {
            HubDescriptor descriptor = null;
            if(_hubProviders.Single(p => p.TryGetHub(hubName, out descriptor)) != null)
            {
                return descriptor;
            }

            return null;
        }

        public ActionDescriptor GetHubAction(string hubName, string actionName, params object[] parameters)
        {
            var hub = GetHub(hubName);
            if (hub == null) return null;

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
            return hub == null 
                ? null 
                : this._actionProviders
                    .SelectMany(p => p.GetActions(hub))
                    .Where(d => predicate == null || predicate(d));
        }

        public IEnumerable<HubDescriptor> GetHubs(Predicate<HubDescriptor> predicate = null)
        {
            return _hubProviders
                .SelectMany(p => p.GetHubs())
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

        #endregion
    }
}