// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;

namespace Microsoft.AspNet.SignalR.Stress
{
    [Export("MemoryHostHubInvocation", typeof(IRun))]
    public class MemoryHostHubInvocationRun : MemoryHostRun
    {
        [ImportingConstructor]
        public MemoryHostHubInvocationRun(RunData runData)
            : base(InitializeHubInvocationPayload(runData))
        {
        }

        public override string Endpoint
        {
            get { return "signalr"; }
        }

        protected override void ConfigureApp(IAppBuilder app)
        {
            var config = new HubConfiguration
            {
                Resolver = Resolver
            };

            app.MapHubs(config);

            config.Resolver.Register(typeof(IProtectedData), () => new EmptyProtectedData());
        }

        protected override IPerformanceCounter[] GetPerformanceCounters(IPerformanceCounterManager counterManager)
        {
            return new[] { counterManager.MessageBusMessagesPublishedPerSec, counterManager.MessageBusMessagesPublishedTotal };
        }

        private static RunData InitializeHubInvocationPayload(RunData runData)
        {
            var jsonSerializer = new JsonSerializer();

            var hubInvocation = new HubInvocation
            {
                Hub = "EchoHub",
                Method = "Echo",
                Args = new[] { JToken.FromObject(runData.Payload, jsonSerializer) },
                CallbackId = ""
            };

            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                jsonSerializer.Serialize(writer, hubInvocation);
                runData.Payload = writer.ToString();
            }

            return runData;
        }
    }
}
