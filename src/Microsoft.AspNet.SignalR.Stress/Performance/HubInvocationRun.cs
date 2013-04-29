// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Stress.Performance;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Owin;

namespace Microsoft.AspNet.SignalR.Stress
{
    [Export("HubInvocation", typeof(IRun))]
    public class HubInvocationRun : SendReceiveRun
    {
        [ImportingConstructor]
        public HubInvocationRun(RunData runData)
            : base(InitializeHubInvocationPayload(runData))
        {
        }

        public override string Endpoint
        {
            get { return "signalr"; }
        }

        protected override string ScenarioName
        {
            get
            {
                return GetContractName();
            }
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
                Hub = "SimpleEchoHub",
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
