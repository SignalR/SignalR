// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class DefaultHubActivator : IHubActivator
    {
        private readonly IDependencyResolver _resolver;

        public DefaultHubActivator(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public HubActivationResult Create(HubDescriptor descriptor)
        {
            if(descriptor.Type == null)
            {
                return null;
            }

            object hub = _resolver.Resolve(descriptor.Type) ?? Activator.CreateInstance(descriptor.Type);
            return new HubActivationResult(hub as IHub);
        }
    }
}
