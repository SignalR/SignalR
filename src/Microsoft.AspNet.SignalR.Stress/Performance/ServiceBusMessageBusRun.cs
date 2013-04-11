// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Redis;
using Microsoft.AspNet.SignalR.ServiceBus;

namespace Microsoft.AspNet.SignalR.Stress.Performance
{
    [Export("ServiceBusMessageBus", typeof(IRun))]
    public class ServiceBusMessageBusRun : MessageBusRun
    {
        [ImportingConstructor]
        public ServiceBusMessageBusRun(RunData runData)
            : base(runData)
        {

        }

        protected override MessageBus CreateMessageBus()
        {
            var configuration = new ServiceBusScaleoutConfiguration(RunData.ServiceBusConnectionString, "Stress");
            // configuration.RetryOnError = true;

            return new ServiceBusMessageBus(Resolver, configuration);
        }
    }
}
