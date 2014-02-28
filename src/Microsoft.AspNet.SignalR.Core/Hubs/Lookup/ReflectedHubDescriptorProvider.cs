// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class ReflectedHubDescriptorProvider : IHubDescriptorProvider
    {
        private readonly Lazy<IDictionary<string, HubDescriptor>> _hubs;
        private readonly Lazy<IAssemblyLocator> _locator;
        private readonly TraceSource _trace;

        public ReflectedHubDescriptorProvider(IDependencyResolver resolver)
        {
            _locator = new Lazy<IAssemblyLocator>(resolver.Resolve<IAssemblyLocator>);
            _hubs = new Lazy<IDictionary<string, HubDescriptor>>(BuildHubsCache);


            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(ReflectedHubDescriptorProvider).Name];
        }

        public IList<HubDescriptor> GetHubs()
        {
            return _hubs.Value
                .Select(kv => kv.Value)
                .Distinct()
                .ToList();
        }

        public bool TryGetHub(string hubName, out HubDescriptor descriptor)
        {
            return _hubs.Value.TryGetValue(hubName, out descriptor);
        }

        protected IDictionary<string, HubDescriptor> BuildHubsCache()
        {
            // Getting all IHub-implementing types that apply
            var types = _locator.Value.GetAssemblies()
                                      .SelectMany(GetTypesSafe)
                                      .Where(IsHubType);

            // Building cache entries for each descriptor
            // Each descriptor is stored in dictionary under a key
            // that is it's name or the name provided by an attribute
            var hubDescriptors = types
                .Select(type => new HubDescriptor
                                {
                                    NameSpecified = (type.GetHubAttributeName() != null),
                                    Name = type.GetHubName(),
                                    HubType = type
                                });

            var cacheEntries = new Dictionary<string, HubDescriptor>(StringComparer.OrdinalIgnoreCase);

            foreach (var descriptor in hubDescriptors)
            {
                HubDescriptor oldDescriptor = null;
                if (!cacheEntries.TryGetValue(descriptor.Name, out oldDescriptor))
                {
                    cacheEntries[descriptor.Name] = descriptor;
                }
                else
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                            Resources.Error_DuplicateHubNames,
                            oldDescriptor.HubType.AssemblyQualifiedName,
                            descriptor.HubType.AssemblyQualifiedName,
                            descriptor.Name));
                }
            }

            return cacheEntries;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "If we throw then it's not a hub type")]
        private static bool IsHubType(Type type)
        {
            try
            {
                return typeof(IHub).IsAssignableFrom(type) &&
                       !type.IsAbstract &&
                       (type.Attributes.HasFlag(TypeAttributes.Public) ||
                        type.Attributes.HasFlag(TypeAttributes.NestedPublic));
            }
            catch
            {
                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "If we throw then we have an empty type")]
        private IEnumerable<Type> GetTypesSafe(Assembly a)
        {
            try
            {
                return a.GetTypes();
            }
            catch (Exception ex)
            {
                _trace.TraceWarning(Resources.Warning_AssemblyGetTypesException,
                                    a.FullName,
                                    a.Location,
                                    ex.GetType().Name,
                                    ex.Message);

                return Enumerable.Empty<Type>();
            }
        }
    }
}
