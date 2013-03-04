// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Knockout
{
    public static class DependencyResolverExtensions
    {
        internal const string KoSignalPrefix = "ko-";
        internal const string GetStateCommand = "GetState";

        private static object _initMethodDescriptorProviderLock = new object();
        private static bool _methodDescriptorProvierInitialized = false;

        public static void ActivateKnockoutHub<T>(this IDependencyResolver resolver,
                                                  object initialState) 
            where T : KnockoutHub
        {
            var bus = resolver.Resolve<IMessageBus>();
            var serializer = resolver.Resolve<IJsonSerializer>();
            var connectionManager = resolver.Resolve<IConnectionManager>();

            resolver.EnsureRegistered(bus, serializer);

            var hubType = typeof(T);
            string hubName = GetHubAttributeName(hubType) ?? hubType.Name;
            IHubContext hubContext = connectionManager.GetHubContext(hubName);
            var jsonMerger = new JsonMerger(JObject.FromObject(initialState));

            // TODO: Ensure only one subscriber exists per key in scale out scenario
            var diffSubscriber = new DiffSubscriber(bus,
                                                    serializer,
                                                    KoSignalPrefix + hubName,
                                                    DiffHandler(hubContext, jsonMerger),
                                                    CommandHandler(hubContext, jsonMerger));
        }

        private static void EnsureRegistered(this IDependencyResolver resolver,
                                             IMessageBus bus,
                                             IJsonSerializer serializer)
        {
            lock (_initMethodDescriptorProviderLock)
            {
                if (!_methodDescriptorProvierInitialized)
                {
                    var knockoutMethodDescriptorProvider = new Lazy<IMethodDescriptorProvider>(() =>
                                                               new KnockoutMethodDescriptorProvider(bus, serializer));
                    resolver.Register(typeof(IMethodDescriptorProvider), () => knockoutMethodDescriptorProvider.Value);
                    _methodDescriptorProvierInitialized = true;
                }
            }
        }

        private static Func<string, JRaw, Task> DiffHandler(IHubContext hubContext, JsonMerger jsonMerger)
        {
            return (source, diff) =>
            {
                var parsedDiff = JToken.Parse(diff.ToString());

                jsonMerger.Merge(parsedDiff);
                hubContext.Clients.AllExcept(source).onKnockoutUpdate(diff, replace: false);

                return TaskAsyncHelper.Empty;
            };
        }

        private static Func<Message, Task> CommandHandler(IHubContext hubContext, JsonMerger jsonMerger)
        {
            return (message) =>
            {
                if (message.CommandId == GetStateCommand)
                {
                    hubContext.Clients.Client(message.Source).onKnockoutUpdate(jsonMerger.State, replace: true);
                }
                return TaskAsyncHelper.Empty;
            };
        }

        // Taken from HubTypeExtensions. Perhaps this should be made public? 
        private static string GetHubAttributeName(this Type type)
        {
            // We can still return null if there is no attribute name
            return ReflectionHelper.GetAttributeValue<HubNameAttribute, string>(type, attr => attr.HubName);
        }
    }
}
