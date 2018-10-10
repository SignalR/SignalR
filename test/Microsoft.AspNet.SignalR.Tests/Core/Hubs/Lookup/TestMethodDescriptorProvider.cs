// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Json;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    internal class TestMethodDescriptorProvider: IMethodDescriptorProvider
    {
        private readonly IDictionary<string, MethodDescriptor> _methods;
        private readonly string _hubName;

        public TestMethodDescriptorProvider(string hubName, params MethodDescriptor[] methods)
        {
            _methods = methods.ToDictionary(h => h.Name);
            _hubName = hubName;
        }

        public IEnumerable<MethodDescriptor> GetMethods(HubDescriptor hub)
        {
            if(hub.Name.Equals(_hubName))
            {
                return _methods.Values;
            }
            return Enumerable.Empty<MethodDescriptor>();
        }

        public bool TryGetMethod(HubDescriptor hub, string method, out MethodDescriptor descriptor, IList<IJsonValue> parameters)
        {
            if(hub.Name.Equals(_hubName) && _methods.TryGetValue(method, out var candidate))
            {
                if(candidate.Parameters.Count == parameters.Count)
                {
                    descriptor = candidate;
                    return true;
                }
            }

            descriptor = null;
            return false;
        }
    }
}
