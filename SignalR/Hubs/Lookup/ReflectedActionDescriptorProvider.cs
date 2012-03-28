using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SignalR.Hubs.Extensions;
using SignalR.Hubs.Lookup.Descriptors;
using SignalR.Infrastructure;

namespace SignalR.Hubs.Lookup
{
    using System.Reflection;

    public class ReflectedActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly ConcurrentDictionary<string, IDictionary<string, IEnumerable<ActionDescriptor>>> _actions;

        public ReflectedActionDescriptorProvider()
        {
            _actions = new ConcurrentDictionary<string, IDictionary<string, IEnumerable<ActionDescriptor>>>();
        }

        public IEnumerable<ActionDescriptor> GetActions(HubDescriptor hub)
        {
            return BuildActionsDictionary(hub)
                .SelectMany(kv => kv.Value)
                .ToList();
        }

        private IDictionary<string, IEnumerable<ActionDescriptor>> BuildActionsDictionary(HubDescriptor hub)
        {
            return _actions.GetOrAdd(
                hub.Name, 
                key => ReflectionHelper.GetExportedHubMethods(hub.Type)
                    .Select(m => 
                        {
                            var descriptor = new ActionDescriptor 
                                {
                                    ReturnType = m.ReturnType,
                                    Name = GetActionName(m),
                                    Parameters = m.GetParameters()
                                        .Select(p => new ParameterDescriptor 
                                            {
                                                Name = p.Name,
                                                Type = p.ParameterType
                                            })                   
                                };

                            descriptor.Invoker = (target, parameters) => m.Invoke(target, descriptor.Adjust(parameters));
                            return descriptor;
                        })
                    .GroupBy(d => d.Name)
                    .ToDictionary(a => a.Key.ToLowerInvariant(), 
                                  a => a.AsEnumerable()));
        }

        public bool TryGetAction(HubDescriptor hub, string action, out ActionDescriptor descriptor, params object[] parameters)
        {
            IEnumerable<ActionDescriptor> descriptors;
            BuildActionsDictionary(hub).TryGetValue(action.ToLowerInvariant(), out descriptors);

            try
            {
                descriptor = descriptors != null 
                    ? descriptors.SingleOrDefault(d => d.Matches(parameters)) 
                    : null;
            }
            catch(Exception)
            {
                descriptor = null;
            }

            return descriptor != null;
        }

        private static string GetActionName(MethodInfo method)
        {
            return ReflectionHelper.GetAttributeValue<HubMethodNameAttribute, string>(method, a => a.MethodName)
                   ?? method.Name;
        }
    }
}