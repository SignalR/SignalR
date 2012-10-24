﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class DefaultHubManager : IHubManager
    {
        private readonly IEnumerable<IMethodDescriptorProvider> _methodProviders;
        private readonly IHubActivator _activator;
        private readonly IEnumerable<IHubDescriptorProvider> _hubProviders;
        private IHubDeactivator _deactivator;

        public DefaultHubManager(IDependencyResolver resolver)
        {
            _hubProviders = resolver.ResolveAll<IHubDescriptorProvider>();
            _methodProviders = resolver.ResolveAll<IMethodDescriptorProvider>();
            _activator = resolver.Resolve<IHubActivator>();
            _deactivator = resolver.Resolve<IHubDeactivator>();
        }

        public HubDescriptor GetHub(string hubName)
        {
            HubDescriptor descriptor = null;
            if(_hubProviders.FirstOrDefault(p => p.TryGetHub(hubName, out descriptor)) != null)
            {
                return descriptor;
            }

            return null;
        }

        public IEnumerable<HubDescriptor> GetHubs(Func<HubDescriptor, bool> predicate = null)
        {
            var hubs = _hubProviders.SelectMany(p => p.GetHubs());

            if(predicate != null) 
            {
                return hubs.Where(predicate);
            }

            return hubs;
        }

        public MethodDescriptor GetHubMethod(string hubName, string method, params IJsonValue[] parameters)
        {
            HubDescriptor hub = GetHub(hubName);

            if (hub == null)
            {
                return null;
            }

            MethodDescriptor descriptor = null;
            if (_methodProviders.FirstOrDefault(p => p.TryGetMethod(hub, method, out descriptor, parameters)) != null)
            {
                return descriptor;
            }

            return null;
        }

        public IEnumerable<MethodDescriptor> GetHubMethods(string hubName, Func<MethodDescriptor, bool> predicate = null)
        {
            HubDescriptor hub = GetHub(hubName);

            if (hub == null)
            {
                return null;
            }

            var methods = _methodProviders.SelectMany(p => p.GetMethods(hub));

            if(predicate != null) 
            {
                return methods.Where(predicate);
            }

            return methods;
                    
        }

        public void Deactivate(HubActivationResult hubActivationResult)
        {
            hubActivationResult.Hub.Dispose();
            _deactivator.Destruct(hubActivationResult);
        }

        public HubActivationResult ResolveHub(string hubName)
        {
            HubDescriptor hub = GetHub(hubName);
            return hub == null ? null : _activator.Create(hub);
        }

        public IEnumerable<HubActivationResult> ResolveHubs()
        {
            return GetHubs().Select(hub => _activator.Create(hub));
        }
    }
}
