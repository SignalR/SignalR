namespace SignalR.Hubs.Lookup
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using SignalR.Hubs.Extensions;
    using SignalR.Hubs.Lookup.Descriptors;
    using SignalR.Infrastructure;

    /// <summary>
    /// Default hub action descriptor provider - reflection-based.
    /// </summary>
    public class ReflectedActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly ConcurrentDictionary<string, IDictionary<string, IEnumerable<ActionDescriptor>>> _actions;

        public ReflectedActionDescriptorProvider()
        {
            _actions = new ConcurrentDictionary<string, IDictionary<string, IEnumerable<ActionDescriptor>>>();
        }

        public IEnumerable<ActionDescriptor> GetActions(HubDescriptor hub)
        {
            return BuildActionsDictionary(hub).SelectMany(kv => kv.Value).ToList();
        }

        private IDictionary<string, IEnumerable<ActionDescriptor>> BuildActionsDictionary(HubDescriptor hub)
        {
            return _actions.GetOrAdd(
               hub.Name, key => 
               {
                   return ReflectionHelper.GetExportedHubMethods(hub.Type)
                      .Select(m =>
                      {
                          var descriptor = new ActionDescriptor
                          {
                              Name = ReflectionHelper.GetAttributeValue<HubMethodNameAttribute, string>(m, a => a.MethodName) ?? m.Name,
                              Parameters = m.GetParameters().Select(p =>
                                  new ParameterDescriptor
                                  {
                                      Name = p.Name,
                                      Type = p.ParameterType
                                  }),
                              ReturnType = m.ReturnType
                          };

                          descriptor.Invoker = (target, parameters) => m.Invoke(target, descriptor.Adjust(parameters));
                          return descriptor;
                      })
                      .GroupBy(d => d.Name)
                      .ToDictionary(a => a.Key.ToLowerInvariant(), a => a.AsEnumerable());
               });
        }

        public bool TryGetAction(HubDescriptor hub, string action, out ActionDescriptor descriptor, params object[] parameters)
        {
            IEnumerable<ActionDescriptor> descriptors;
            BuildActionsDictionary(hub).TryGetValue(action.ToLowerInvariant(), out descriptors);

            // Find the best matching action.
            try
            {
                descriptor = descriptors != null ? descriptors.SingleOrDefault(d => d.Matches(parameters)) : null;
            }
            catch(Exception)
            {
                descriptor = null;
            }
            return descriptor != null;
        }
    }
}