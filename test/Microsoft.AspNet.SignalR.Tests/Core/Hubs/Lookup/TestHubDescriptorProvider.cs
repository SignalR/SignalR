// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    internal class TestHubDescriptorProvider : IHubDescriptorProvider
    {
        private readonly IDictionary<string, HubDescriptor> _hubs;

        public TestHubDescriptorProvider(params HubDescriptor[] hubs) => _hubs = hubs.ToDictionary(h => h.Name);

        public IList<HubDescriptor> GetHubs() => _hubs.Values.ToList();

        public bool TryGetHub(string hubName, out HubDescriptor descriptor) => _hubs.TryGetValue(hubName, out descriptor);
    }
}
